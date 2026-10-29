using ClosedXML.Excel;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Planillas;

namespace SIGECDC.Infrastructure.Reportes;

public sealed class GeneradorReportePlanillaExcel : IGeneradorReportePlanillaExcel
{
    private const string MimeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string ColorPrincipal = "1C604E";
    private const string ColorResumen = "EBF6F2";
    private const string ColorBorde = "B0CFC5";
    private const string FormatoMoneda = "₡#,##0.00";

    private static readonly string[] Encabezados =
    [
        "Código",
        "Colaborador",
        "Departamento",
        "Salario del período",
        "Horas extra",
        "Bonos",
        "Beneficios",
        "Ausencias",
        "Salario bruto",
        "Deducciones",
        "Salario neto"
    ];

    public ArchivoDescarga Generar(ReportePlanilla reporte)
    {
        UtilidadesReportePlanilla.Validar(reporte);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Planilla");
        hoja.ShowGridLines = false;

        hoja.Range("A1:K1").Merge();
        hoja.Cell("A1").SetValue("Reporte consolidado de planilla");
        hoja.Cell("A1").Style
            .Font.SetBold()
            .Font.SetFontSize(16)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ColorPrincipal))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        hoja.Row(1).Height = 30;

        hoja.Range("A2:K2").Merge();
        hoja.Cell("A2").SetValue($"{reporte.Rotulo} · Ejecución {reporte.IdEjecucionPlanilla} · V{reporte.NumeroVersion}");
        hoja.Cell("A2").Style.Font.SetBold().Font.SetFontColor(XLColor.DarkSlateGray);
        hoja.Row(2).Height = 21;

        EscribirEtiquetaValor(hoja, "A4", "B4", "Código", reporte.CodigoPeriodo);
        EscribirEtiquetaValor(hoja, "D4", "E4", "Período", reporte.NombrePeriodo);
        EscribirEtiquetaValor(hoja, "H4", "I4", "Estado", reporte.EstadoPlanilla);
        EscribirEtiquetaValor(hoja, "A5", "B5", "Tipo", reporte.TipoPeriodo);
        EscribirEtiquetaValor(hoja, "D5", "E5", "Desde", reporte.FechaInicio);
        EscribirEtiquetaValor(hoja, "H5", "I5", "Hasta", reporte.FechaFin);
        EscribirEtiquetaValor(hoja, "A6", "B6", "Calculada", reporte.FechaCalculo);

        hoja.Range("D6:E6").Merge();
        hoja.Cell("D6").SetValue("Aprobada");
        AplicarEtiqueta(hoja.Cell("D6"));
        EscribirFechaOpcional(hoja.Cell("F6"), reporte.FechaAprobacion);

        hoja.Range("H6:I6").Merge();
        hoja.Cell("H6").SetValue("Cerrada");
        AplicarEtiqueta(hoja.Cell("H6"));
        EscribirFechaOpcional(hoja.Cell("J6"), reporte.FechaCierre);

        EscribirTarjetaTotal(hoja, "A8:C8", "A9:C9", "Salario bruto", reporte.SalarioBrutoTotal);
        EscribirTarjetaTotal(hoja, "E8:G8", "E9:G9", "Deducciones", reporte.DeduccionesTotal);
        EscribirTarjetaTotal(hoja, "I8:K8", "I9:K9", "Salario neto", reporte.SalarioNetoTotal);

        const int filaEncabezado = 11;
        for (var indice = 0; indice < Encabezados.Length; indice++)
        {
            hoja.Cell(filaEncabezado, indice + 1).SetValue(Encabezados[indice]);
        }

        var rangoEncabezado = hoja.Range(filaEncabezado, 1, filaEncabezado, Encabezados.Length);
        rangoEncabezado.Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ColorPrincipal))
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Alignment.SetWrapText();
        rangoEncabezado.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        rangoEncabezado.Style.Border.BottomBorderColor = XLColor.FromHtml(ColorPrincipal);
        hoja.Row(filaEncabezado).Height = 32;

        var fila = filaEncabezado + 1;
        foreach (var detalle in reporte.Detalles)
        {
            EscribirTexto(hoja.Cell(fila, 1), detalle.CodigoColaborador);
            EscribirTexto(hoja.Cell(fila, 2), detalle.NombreColaborador);
            EscribirTexto(hoja.Cell(fila, 3), detalle.Departamento);
            EscribirMonto(hoja.Cell(fila, 4), detalle.SalarioProporcional);
            EscribirMonto(hoja.Cell(fila, 5), detalle.TotalHorasExtra);
            EscribirMonto(hoja.Cell(fila, 6), detalle.TotalBonos);
            EscribirMonto(hoja.Cell(fila, 7), detalle.TotalBeneficiosConfigurables);
            EscribirMonto(hoja.Cell(fila, 8), detalle.TotalAusencias);
            EscribirMonto(hoja.Cell(fila, 9), detalle.SalarioBruto);
            EscribirMonto(hoja.Cell(fila, 10), detalle.TotalDeducciones);
            EscribirMonto(hoja.Cell(fila, 11), detalle.SalarioNeto);

            if ((fila - filaEncabezado) % 2 == 0)
            {
                hoja.Range(fila, 1, fila, Encabezados.Length)
                    .Style.Fill.SetBackgroundColor(XLColor.FromHtml("F7F9F8"));
            }

            fila++;
        }

        hoja.Cell(fila, 1).SetValue(reporte.EstadoPlanilla == "Calculada" ? "Totales de revisión" : "Totales oficiales");
        hoja.Range(fila, 1, fila, 8).Merge();
        hoja.Cell(fila, 1).Style.Font.SetBold();
        EscribirMonto(hoja.Cell(fila, 9), reporte.SalarioBrutoTotal);
        EscribirMonto(hoja.Cell(fila, 10), reporte.DeduccionesTotal);
        EscribirMonto(hoja.Cell(fila, 11), reporte.SalarioNetoTotal);
        hoja.Range(fila, 1, fila, Encabezados.Length).Style
            .Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml(ColorResumen));
        hoja.Range(fila, 1, fila, Encabezados.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        hoja.Range(fila, 1, fila, Encabezados.Length).Style.Border.TopBorderColor = XLColor.FromHtml(ColorBorde);

        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.Column(1).Width = 16;
        hoja.Column(2).Width = 32;
        hoja.Column(3).Width = 24;
        for (var columna = 4; columna <= Encabezados.Length; columna++)
        {
            hoja.Column(columna).Width = 17;
        }

        var alineacionTexto = hoja.Range(filaEncabezado + 1, 1, fila - 1, 3).Style.Alignment;
        alineacionTexto.Vertical = XLAlignmentVerticalValues.Center;
        alineacionTexto.WrapText = true;

        var alineacionMontos = hoja.Range(filaEncabezado + 1, 4, fila, Encabezados.Length).Style.Alignment;
        alineacionMontos.Horizontal = XLAlignmentHorizontalValues.Right;
        alineacionMontos.Vertical = XLAlignmentVerticalValues.Center;
        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        hoja.PageSetup.FitToPages(1, 0);
        hoja.PageSetup.Margins.SetLeft(0.3).SetRight(0.3).SetTop(0.5).SetBottom(0.5);
        hoja.PageSetup.SetRowsToRepeatAtTop(filaEncabezado, filaEncabezado);

        AgregarDesgloses(libro, reporte);
        var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        memoria.Position = 0;

        return new ArchivoDescarga(
            memoria,
            $"reporte-planilla-{UtilidadesReportePlanilla.SanearCodigoPeriodo(reporte.CodigoPeriodo)}.xlsx",
            MimeExcel);
    }

    private static void EscribirEtiquetaValor(
        IXLWorksheet hoja,
        string direccionEtiqueta,
        string direccionValor,
        string etiqueta,
        object valor)
    {
        hoja.Cell(direccionEtiqueta).SetValue(etiqueta);
        AplicarEtiqueta(hoja.Cell(direccionEtiqueta));

        switch (valor)
        {
            case DateTime fecha:
                hoja.Cell(direccionValor).SetValue(fecha);
                hoja.Cell(direccionValor).Style.DateFormat.Format = "dd/mm/yyyy";
                break;
            default:
                EscribirTexto(hoja.Cell(direccionValor), valor.ToString() ?? string.Empty);
                break;
        }
    }

    private static void AplicarEtiqueta(IXLCell celda)
    {
        celda.Style.Font.SetBold().Font.SetFontColor(XLColor.DarkSlateGray);
    }

    private static void EscribirFechaOpcional(IXLCell celda, DateTime? fecha)
    {
        if (fecha.HasValue)
        {
            celda.SetValue(fecha.Value);
            celda.Style.DateFormat.Format = "dd/mm/yyyy";
        }
        else
        {
            EscribirTexto(celda, "—");
        }
    }

    private static void EscribirTarjetaTotal(
        IXLWorksheet hoja,
        string rangoEtiqueta,
        string rangoValor,
        string etiqueta,
        decimal monto)
    {
        hoja.Range(rangoEtiqueta).Merge();
        hoja.Range(rangoValor).Merge();
        hoja.Cell(rangoEtiqueta.Split(':')[0]).SetValue(etiqueta);
        hoja.Cell(rangoValor.Split(':')[0]).SetValue(monto);

        var tarjeta = hoja.Range(
            hoja.Range(rangoEtiqueta).FirstCell().Address,
            hoja.Range(rangoValor).LastCell().Address);
        tarjeta.Style.Fill.SetBackgroundColor(XLColor.FromHtml(ColorResumen));
        tarjeta.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tarjeta.Style.Border.OutsideBorderColor = XLColor.FromHtml(ColorBorde);
        tarjeta.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        tarjeta.FirstRow().Style.Font.SetFontColor(XLColor.DarkSlateGray);
        tarjeta.LastRow().Style.Font.SetBold().Font.SetFontSize(12);
        tarjeta.LastRow().Style.NumberFormat.Format = FormatoMoneda;
        hoja.Row(8).Height = 20;
        hoja.Row(9).Height = 24;
    }

    private static void EscribirTexto(IXLCell celda, string? valor)
    {
        celda.SetValue(string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim());
        celda.Style.NumberFormat.Format = "@";
    }

    private static void EscribirMonto(IXLCell celda, decimal monto)
    {
        EscribirNumeroExacto(celda, monto);
        celda.Style.NumberFormat.Format = FormatoMoneda;
    }

    private static void EscribirNumeroExacto(IXLCell celda, decimal valor)
    {
        celda.SetValue(valor);
        if (celda.GetValue<decimal>() != valor)
            throw new InvalidOperationException("EXPORTACION_PRECISION_NO_REPRESENTABLE");
    }

    private static void AgregarDesgloses(XLWorkbook libro, ReportePlanilla reporte)
    {
        IXLWorksheet Hoja(string nombre, string[] columnas)
        {
            var h = libro.Worksheets.Add(nombre); h.ShowGridLines = false;
            h.Cell(1, 1).SetValue(reporte.Rotulo); h.Cell(2, 1).SetValue($"Ejecución {reporte.IdEjecucionPlanilla} · V{reporte.NumeroVersion}");
            h.Cell(3, 1).SetValue(reporte.FechaInicio); h.Cell(3, 2).SetValue(reporte.FechaFin);
            h.Range(3, 1, 3, 2).Style.DateFormat.Format = "dd/mm/yyyy";
            for (var i = 0; i < columnas.Length; i++) h.Cell(5, i + 1).SetValue(columnas[i]);
            h.Range(5, 1, 5, columnas.Length).Style.Font.SetBold(); h.SheetView.FreezeRows(5);
            h.Columns(1, columnas.Length).Width = 23; h.Column(2).Width = 36;
            h.PageSetup.PageOrientation = XLPageOrientation.Landscape; h.PageSetup.FitToPages(1, 0);
            h.PageSetup.SetRowsToRepeatAtTop(1, 5); return h;
        }
        var lineas = Hoja("Conceptos", ["Código", "Colaborador", "Concepto", "Clasificación", "Efecto", "Fecha", "Cantidad", "Unidad", "Monto"]);
        var segmentos = Hoja("Segmentos", ["Código", "Colaborador", "Desde", "Hasta", "Puesto", "Departamento", "Contrato", "Jornada", "Horas/día", "Salario mensual", "Días base 30"]);
        var resumen = Hoja("Agregados", ["Código", "Colaborador", "Bruto salarial", "No salariales", "Bruto pagadero", "Cuotas obreras", "Renta", "Otras deducciones", "Deducciones", "Neto", "Carga patronal", "RT", "Otros costos", "Costo laboral total"]);
        var f = 6; var s = 6; var r = 6;
        foreach (var d in reporte.Detalles)
        {
            resumen.Cell(r, 1).SetValue(d.CodigoColaborador); resumen.Cell(r, 2).SetValue(d.NombreColaborador);
            decimal[] valores = [d.SalarioBruto, d.IngresosNoSalariales, d.BrutoPagadero, d.CuotasObreras, d.ImpuestoSalario,
                d.OtrasDeducciones, d.TotalDeducciones, d.SalarioNeto, d.CargasPatronales, d.RiesgosTrabajo, d.OtrosCostosPatronales, d.CostoLaboralTotal];
            for (var i = 0; i < valores.Length; i++) EscribirMonto(resumen.Cell(r, i + 3), valores[i]); r++;
            foreach (var l in d.Lineas)
            {
                lineas.Cell(f, 1).SetValue(d.CodigoColaborador); lineas.Cell(f, 2).SetValue(d.NombreColaborador);
                lineas.Cell(f, 3).SetValue(l.Descripcion); lineas.Cell(f, 4).SetValue(l.Clasificacion); lineas.Cell(f, 5).SetValue(l.EtiquetaEfecto);
                if (l.Fecha is { } fecha) { lineas.Cell(f, 6).SetValue(fecha); lineas.Cell(f, 6).Style.DateFormat.Format = "dd/mm/yyyy"; }
                if (l.Cantidad is { } cantidad) EscribirNumeroExacto(lineas.Cell(f, 7), cantidad);
                lineas.Cell(f, 8).SetValue(l.Unidad ?? ""); EscribirMonto(lineas.Cell(f, 9), l.Monto); f++;
            }
            foreach (var tramo in d.Segmentos)
            {
                segmentos.Cell(s, 1).SetValue(d.CodigoColaborador); segmentos.Cell(s, 2).SetValue(d.NombreColaborador);
                segmentos.Cell(s, 3).SetValue(tramo.Inicio); segmentos.Cell(s, 4).SetValue(tramo.Fin);
                segmentos.Range(s, 3, s, 4).Style.DateFormat.Format = "dd/mm/yyyy";
                segmentos.Cell(s, 5).SetValue(tramo.Puesto); segmentos.Cell(s, 6).SetValue(tramo.Departamento);
                segmentos.Cell(s, 7).SetValue(tramo.TipoContrato); segmentos.Cell(s, 8).SetValue(tramo.Jornada);
                EscribirNumeroExacto(segmentos.Cell(s, 9), tramo.HorasDia); EscribirMonto(segmentos.Cell(s, 10), tramo.SalarioMensual);
                EscribirNumeroExacto(segmentos.Cell(s, 11), tramo.DiasBase30); s++;
            }
        }
        resumen.Cell(r, 1).SetValue("Totales de ejecución");
        foreach (var par in new[] { (3, reporte.SalarioBrutoTotal), (4, reporte.IngresosNoSalarialesTotal), (5, reporte.BrutoPagaderoTotal),
            (9, reporte.DeduccionesTotal), (10, reporte.SalarioNetoTotal), (11, reporte.CargasPatronalesTotal), (12, reporte.RiesgosTrabajoTotal),
            (13, reporte.OtrosCostosPatronalesTotal), (14, reporte.CostoLaboralTotal) }) EscribirMonto(resumen.Cell(r, par.Item1), par.Item2);
    }
}
