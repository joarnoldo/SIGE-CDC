using System.Globalization;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Infrastructure.Reportes;

// Ambos canales presentan exclusivamente la proyección persistida de M. Sin servicios de datos ni cálculos.
public sealed class GeneradorComparacionForecast:IGeneradorComparacionForecast
{
    private static readonly CultureInfo Cultura=CultureInfo.GetCultureInfo("es-CR");
    private static string Numero(decimal? n,int decimales=2)=>n?.ToString("N"+decimales,Cultura)??"No disponible";
    public ArchivoDescarga Excel(ConsultaComparacionPersistida consulta)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        using var libro=new XLWorkbook();
        var hoja=libro.Worksheets.Add("Comparacion");var dimensiones=libro.Worksheets.Add("Dimensiones historicas");
        foreach(var h in new[]{hoja,dimensiones})
        {h.Cell(1,1).SetValue(consulta.Nombre);h.Cell(2,1).SetValue(consulta.Estado+" · Comparación histórica de snapshots · FIN-02 contenido");h.SheetView.FreezeRows(4);}
        string[] titulos=["Período","Inicio","Fin","Ejecución real","Versión real","Estado real observado","Disponibilidad","Dimensión","Código Forecast","Forecast histórico","Código real","Real histórico","Monto Forecast","Monto real","Diferencia firmada","Magnitud absoluta","Diferencia %","Motivo"];
        for(var i=0;i<titulos.Length;i++)hoja.Cell(4,i+1).SetValue(titulos[i]);
        string[] dims=["Período","Participante Forecast","Participante real","Lado","Inicio","Fin","Puesto","Departamento","Jornada","Salario mensual"];
        for(var i=0;i<dims.Length;i++)dimensiones.Cell(4,i+1).SetValue(dims[i]);
        var fila=5;var dim=5;
        foreach(var p in consulta.Periodos)
        {
            void Cabecera(int f)
            {
                hoja.Cell(f,1).SetValue(p.TipoPeriodo);hoja.Cell(f,2).SetValue(p.Inicio);hoja.Cell(f,3).SetValue(p.Fin);
                hoja.Range(f,2,f,3).Style.DateFormat.Format="yyyy-mm-dd";
                if(p.IdEjecucionReal is {} id)hoja.Cell(f,4).SetValue("EJ-"+id.ToString(CultureInfo.InvariantCulture));
                if(p.VersionReal is {} v)hoja.Cell(f,5).SetValue(v);
                hoja.Cell(f,6).SetValue(p.EstadoReal??"");hoja.Cell(f,7).SetValue(p.Disponibilidad);
            }
            if(p.Filas.Count==0){Cabecera(fila);hoja.Cell(fila,18).SetValue(p.Motivo??"");fila++;}
            foreach(var r in p.Filas)
            {
                Cabecera(fila);hoja.Cell(fila,8).SetValue(r.Granularidad);hoja.Cell(fila,9).SetValue(r.CodigoForecast);hoja.Cell(fila,10).SetValue(r.NombreForecast);
                hoja.Cell(fila,11).SetValue(r.CodigoReal);hoja.Cell(fila,12).SetValue(r.NombreReal);
                decimal?[] valores=[r.MontoForecast,r.MontoReal,r.DiferenciaFirmada,r.DiferenciaAbsoluta,r.DiferenciaPorcentual];
                for(var i=0;i<valores.Length;i++)if(valores[i] is {} n)Escribir(hoja.Cell(fila,13+i),n,i==4?"0.0000":"#,##0.00");
                hoja.Cell(fila,18).SetValue(r.Motivo??"");fila++;
                foreach(var d in r.Dimensiones)
                {
                    dimensiones.Cell(dim,1).SetValue(p.TipoPeriodo);dimensiones.Cell(dim,2).SetValue(r.NombreForecast);dimensiones.Cell(dim,3).SetValue(r.NombreReal);
                    dimensiones.Cell(dim,4).SetValue(d.Lado);dimensiones.Cell(dim,5).SetValue(d.Inicio);dimensiones.Cell(dim,6).SetValue(d.Fin);dimensiones.Range(dim,5,dim,6).Style.DateFormat.Format="yyyy-mm-dd";
                    dimensiones.Cell(dim,7).SetValue(d.Puesto);dimensiones.Cell(dim,8).SetValue(d.Departamento);dimensiones.Cell(dim,9).SetValue(d.Jornada);Escribir(dimensiones.Cell(dim,10),d.SalarioMensual,"#,##0.00");dim++;
                }
            }
        }
        foreach(var h in new[]{hoja,dimensiones})
        {h.Row(4).Style.Font.Bold=true;h.Row(4).Style.Fill.BackgroundColor=XLColor.LightGray;h.Columns().AdjustToContents(8,45);h.RowsUsed().Style.Alignment.WrapText=true;}
        var memoria=new MemoryStream();libro.SaveAs(memoria);memoria.Position=0;
        return new(memoria,$"Forecast-{consulta.IdEscenario}-comparacion.xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
    private static void Escribir(IXLCell celda,decimal n,string formato)
    {celda.SetValue(n);if(celda.GetValue<decimal>()!=n)throw new InvalidOperationException("EXPORTACION_PRECISION_NO_REPRESENTABLE");celda.Style.NumberFormat.Format=formato;}

    public ArchivoDescarga Pdf(ConsultaComparacionPersistida consulta)
    {
        ArgumentNullException.ThrowIfNull(consulta);ConfiguracionFuentesPdf.AsegurarInicializacion();
        using var pdf=new PdfDocument();pdf.Info.Title="Comparación Forecast vs Planilla";
        var fuente=new XFont(FuenteNotoSansResolver.NombreFamilia,10);var negrita=new XFont(FuenteNotoSansResolver.NombreFamilia,11,XFontStyleEx.Bold);
        XGraphics? g=null;double y=0;PdfPage? pagina=null;
        void NuevaPagina()
        {
            g?.Dispose();pagina=pdf.AddPage();pagina.Size=PdfSharp.PageSize.A4;g=XGraphics.FromPdfPage(pagina);y=38;
            g.DrawString("SIGE-CDC · Comparación Forecast vs Planilla",negrita,XBrushes.Black,new XPoint(35,y));y+=18;
            g.DrawString(consulta.Estado+" · Snapshot histórico · FIN-02 contenido",fuente,XBrushes.Black,new XPoint(35,y));y+=22;
        }
        void Texto(string texto,bool fuerte=false)
        {
            // Ajuste por ancho medido: no truncar nombres, códigos, motivos ni importes.
            var f=fuerte?negrita:fuente;var resto=texto.Replace('\r',' ').Replace('\n',' ');
            while(resto.Length>0)
            {
                if(g is null || y>pagina!.Height.Point-40)NuevaPagina();
                var largo=resto.Length;while(largo>1 && g!.MeasureString(resto[..largo],f).Width>pagina!.Width.Point-70)largo--;
                if(largo<resto.Length){var espacio=resto.LastIndexOf(' ',largo-1,largo);if(espacio>0)largo=espacio;}
                g!.DrawString(resto[..largo],f,XBrushes.Black,new XPoint(35,y));y+=15;resto=resto[largo..].TrimStart();
            }
        }
        NuevaPagina();Texto(consulta.Nombre,true);
        foreach(var p in consulta.Periodos)
        {
            y+=10;Texto($"{p.TipoPeriodo} · {p.Inicio:yyyy-MM-dd} a {p.Fin:yyyy-MM-dd}",true);
            Texto(p.IdEjecucionReal is {} id?$"Real EJ-{id} · V{p.VersionReal} · {p.VersionMotorReal} · estado observado {p.EstadoReal}":p.Disponibilidad+" · "+p.Motivo);
            foreach(var r in p.Filas)
            {
                // Mantener identificación e importes juntos en los bloques ordinarios.
                if(y+160>pagina!.Height.Point-40)NuevaPagina();
                y+=8;Texto(r.Granularidad,true);
                Texto("Forecast: "+(r.TieneForecast?r.CodigoForecast+" · "+r.NombreForecast:"Sin correspondencia"));
                Texto("Real: "+(r.TieneReal?r.CodigoReal+" · "+r.NombreReal:"Sin correspondencia"));
                Texto("CRC Forecast: "+Numero(r.MontoForecast)+" | Real: "+Numero(r.MontoReal));
                Texto("Diferencia: "+Numero(r.DiferenciaFirmada)+" | Magnitud: "+Numero(r.DiferenciaAbsoluta)+" | %: "+Numero(r.DiferenciaPorcentual,4));
                if(r.Motivo is {} motivo)Texto("Motivo: "+motivo);
                foreach(var d in r.Dimensiones)Texto($"{d.Lado} {d.Inicio:yyyy-MM-dd}–{d.Fin:yyyy-MM-dd}: {d.Puesto} / {d.Departamento} / {d.Jornada} / salario CRC {Numero(d.SalarioMensual)}");
            }
        }
        g?.Dispose();var memoria=new MemoryStream();pdf.Save(memoria,false);memoria.Position=0;
        return new(memoria,$"Forecast-{consulta.IdEscenario}-comparacion.pdf","application/pdf");
    }
}
