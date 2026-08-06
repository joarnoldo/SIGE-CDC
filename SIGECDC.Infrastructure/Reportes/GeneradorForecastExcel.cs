using ClosedXML.Excel;
using SIGECDC.Application.Archivos;
using SIGECDC.Application.Forecast;

namespace SIGECDC.Infrastructure.Reportes;

public sealed class GeneradorForecastExcel : IGeneradorForecastExcel
{
    private const string MimeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string ColorPrincipal = "1C604E";
    private const string ColorResumen = "EBF6F2";
    private const string ColorBorde = "B0CFC5";
    private const string FormatoMoneda = "₡#,##0.00";

    private static readonly string[] EncabezadosResultados =
    [
        "Clave",
        "Código",
        "Agrupación",
        "Detalle",
        "Tipo de participante",
        "Participantes",
        "Salario bruto",
        "Deducciones",
        "Salario neto"
    ];

    private static readonly string[] EncabezadosConceptos =
    [
        "Clave de agrupación",
        "Código de agrupación",
        "Agrupación",
        "Detalle",
        "Código de concepto",
        "Concepto",
        "Base",
        "Ajuste",
        "Proyectado"
    ];

    public ArchivoDescarga Generar(ConsultaResultadosForecast consulta)
    {
        UtilidadesExportacionForecast.Validar(consulta);

        using var libro = new XLWorkbook();
        libro.Properties.Title = $"Resultados forecast {consulta.IdForecastEscenario}";
        libro.Properties.Subject = "Resultados proyectados del forecast";
        libro.Properties.Author = "Canales y Drenajes del Caribe S.R.L.";

        CrearHojaResultados(libro, consulta);
        CrearHojaConceptos(libro, consulta);

        var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        memoria.Position = 0;

        return new ArchivoDescarga(
            memoria,
            UtilidadesExportacionForecast.CrearNombreArchivo(consulta, "xlsx"),
            MimeExcel);
    }

    private static void CrearHojaResultados(XLWorkbook libro, ConsultaResultadosForecast consulta)
    {
        var hoja = libro.Worksheets.Add("Resultados");
        hoja.ShowGridLines = false;
        EscribirEncabezadoDocumento(hoja, consulta, EncabezadosResultados.Length);

        const int filaEncabezado = 11;
        EscribirEncabezados(hoja, filaEncabezado, EncabezadosResultados);

        var filaActual = filaEncabezado + 1;
        foreach (var fila in consulta.Filas)
        {
            EscribirTexto(hoja.Cell(filaActual, 1), fila.Clave);
            EscribirTexto(hoja.Cell(filaActual, 2), fila.Codigo);
            EscribirTexto(hoja.Cell(filaActual, 3), fila.Etiqueta);
            EscribirTexto(hoja.Cell(filaActual, 4), fila.Detalle);
            EscribirTexto(hoja.Cell(filaActual, 5), TextoTipoParticipante(fila.TipoParticipante));
            hoja.Cell(filaActual, 6).SetValue(fila.CantidadParticipantes);
            hoja.Cell(filaActual, 6).Style.NumberFormat.Format = "#,##0";
            EscribirMonto(hoja.Cell(filaActual, 7), fila.SalarioBrutoProyectado);
            EscribirMonto(hoja.Cell(filaActual, 8), fila.DeduccionesProyectadas);
            EscribirMonto(hoja.Cell(filaActual, 9), fila.SalarioNetoProyectado);

            if ((filaActual - filaEncabezado) % 2 == 0)
            {
                hoja.Range(filaActual, 1, filaActual, EncabezadosResultados.Length)
                    .Style.Fill.SetBackgroundColor(XLColor.FromHtml("F7F9F8"));
            }

            filaActual++;
        }

        hoja.Cell(filaActual, 1).SetValue("Totales de la consulta");
        hoja.Range(filaActual, 1, filaActual, 6).Merge();
        EscribirMonto(hoja.Cell(filaActual, 7), consulta.MontoProyectadoTotalConsulta);
        EscribirMonto(hoja.Cell(filaActual, 8), consulta.DeduccionesTotalConsulta);
        EscribirMonto(hoja.Cell(filaActual, 9), consulta.SalarioNetoTotalConsulta);
        hoja.Range(filaActual, 1, filaActual, EncabezadosResultados.Length).Style
            .Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml(ColorResumen));
        hoja.Range(filaActual, 1, filaActual, EncabezadosResultados.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        hoja.Range(filaActual, 1, filaActual, EncabezadosResultados.Length).Style.Border.TopBorderColor = XLColor.FromHtml(ColorBorde);

        hoja.Range(filaEncabezado, 1, filaActual - 1, EncabezadosResultados.Length).SetAutoFilter();
        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.Column(1).Width = 24;
        hoja.Column(2).Width = 18;
        hoja.Column(3).Width = 31;
        hoja.Column(4).Width = 34;
        hoja.Column(5).Width = 23;
        hoja.Column(6).Width = 15;
        hoja.Columns(7, 9).Width = 18;
        hoja.Range(filaEncabezado + 1, 1, filaActual - 1, 5).Style.Alignment.WrapText = true;
        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        hoja.PageSetup.FitToPages(1, 0);
        hoja.PageSetup.SetRowsToRepeatAtTop(filaEncabezado, filaEncabezado);
    }

    private static void CrearHojaConceptos(XLWorkbook libro, ConsultaResultadosForecast consulta)
    {
        var hoja = libro.Worksheets.Add("Conceptos");
        hoja.ShowGridLines = false;

        hoja.Range("A1:I1").Merge();
        hoja.Cell("A1").SetValue("Desglose de conceptos del forecast");
        AplicarTitulo(hoja.Range("A1:I1"));
        hoja.Range("A2:I2").Merge();
        hoja.Cell("A2").SetValue($"Escenario {consulta.IdForecastEscenario} - {consulta.NombreEscenario}");
        hoja.Cell("A2").Style.Font.SetBold().Font.SetFontColor(XLColor.DarkSlateGray);

        var periodo = UtilidadesExportacionForecast.ObtenerPeriodoAplicado(consulta);
        hoja.Range("A3:I3").Merge();
        hoja.Cell("A3").SetValue(
            $"{UtilidadesExportacionForecast.NombreDimension(consulta.Dimension)} - {TextoAlcance(periodo)}");
        hoja.Cell("A3").Style.Font.SetFontColor(XLColor.DarkSlateGray);

        const int filaEncabezado = 5;
        EscribirEncabezados(hoja, filaEncabezado, EncabezadosConceptos);
        var filaActual = filaEncabezado + 1;

        foreach (var fila in consulta.Filas)
        {
            foreach (var concepto in fila.Conceptos)
            {
                EscribirTexto(hoja.Cell(filaActual, 1), fila.Clave);
                EscribirTexto(hoja.Cell(filaActual, 2), fila.Codigo);
                EscribirTexto(hoja.Cell(filaActual, 3), fila.Etiqueta);
                EscribirTexto(hoja.Cell(filaActual, 4), fila.Detalle);
                EscribirTexto(hoja.Cell(filaActual, 5), concepto.Codigo);
                EscribirTexto(hoja.Cell(filaActual, 6), concepto.Nombre);
                EscribirMonto(hoja.Cell(filaActual, 7), concepto.MontoBase);
                EscribirMonto(hoja.Cell(filaActual, 8), concepto.MontoAjuste);
                EscribirMonto(hoja.Cell(filaActual, 9), concepto.MontoProyectado);

                if ((filaActual - filaEncabezado) % 2 == 0)
                {
                    hoja.Range(filaActual, 1, filaActual, EncabezadosConceptos.Length)
                        .Style.Fill.SetBackgroundColor(XLColor.FromHtml("F7F9F8"));
                }

                filaActual++;
            }
        }

        hoja.Range(filaEncabezado, 1, filaActual - 1, EncabezadosConceptos.Length).SetAutoFilter();
        hoja.SheetView.FreezeRows(filaEncabezado);
        hoja.Columns(1, 2).Width = 23;
        hoja.Column(3).Width = 31;
        hoja.Column(4).Width = 34;
        hoja.Column(5).Width = 25;
        hoja.Column(6).Width = 31;
        hoja.Columns(7, 9).Width = 18;
        hoja.Range(filaEncabezado + 1, 1, filaActual - 1, 6).Style.Alignment.WrapText = true;
        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        hoja.PageSetup.FitToPages(1, 0);
        hoja.PageSetup.SetRowsToRepeatAtTop(filaEncabezado, filaEncabezado);
    }

    private static void EscribirEncabezadoDocumento(
        IXLWorksheet hoja,
        ConsultaResultadosForecast consulta,
        int cantidadColumnas)
    {
        hoja.Range(1, 1, 1, cantidadColumnas).Merge();
        hoja.Cell(1, 1).SetValue("Resultados del forecast");
        AplicarTitulo(hoja.Range(1, 1, 1, cantidadColumnas));

        hoja.Range(2, 1, 2, cantidadColumnas).Merge();
        hoja.Cell(2, 1).SetValue("Canales y Drenajes del Caribe S.R.L.");
        hoja.Cell(2, 1).Style.Font.SetBold().Font.SetFontColor(XLColor.DarkSlateGray);

        EscribirEtiquetaValor(hoja, "A4", "B4", "Escenario", consulta.IdForecastEscenario);
        EscribirEtiquetaValor(hoja, "D4", "E4", "Nombre", consulta.NombreEscenario);
        EscribirEtiquetaValor(hoja, "G4", "H4", "Estado", consulta.EstadoEscenario);
        EscribirEtiquetaValor(
            hoja,
            "A5",
            "B5",
            "Calculado",
            consulta.FechaCalculo!.Value);
        EscribirEtiquetaValor(
            hoja,
            "D5",
            "E5",
            "Dimensión",
            UtilidadesExportacionForecast.NombreDimension(consulta.Dimension));
        EscribirEtiquetaValor(
            hoja,
            "G5",
            "H5",
            "Alcance",
            TextoAlcance(UtilidadesExportacionForecast.ObtenerPeriodoAplicado(consulta)));

        EscribirTarjeta(hoja, "A7:B7", "A8:B8", "Salario bruto", consulta.MontoProyectadoTotalConsulta);
        EscribirTarjeta(hoja, "D7:E7", "D8:E8", "Deducciones", consulta.DeduccionesTotalConsulta);
        EscribirTarjeta(hoja, "G7:H7", "G8:H8", "Salario neto", consulta.SalarioNetoTotalConsulta);

        if (consulta.EsConsistenteConEscenario == false)
        {
            hoja.Range(9, 1, 9, cantidadColumnas).Merge();
            hoja.Cell(9, 1).SetValue(
                "Advertencia: la suma visible no coincide con el total persistido del escenario.");
            hoja.Cell(9, 1).Style
                .Font.SetBold()
                .Font.SetFontColor(XLColor.DarkGoldenrod)
                .Fill.SetBackgroundColor(XLColor.FromHtml("FFF7E0"));
        }
    }

    private static void AplicarTitulo(IXLRange rango)
    {
        rango.Style
            .Font.SetBold()
            .Font.SetFontSize(16)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ColorPrincipal))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        rango.Worksheet.Row(rango.FirstRow().RowNumber()).Height = 30;
    }

    private static void EscribirEncabezados(IXLWorksheet hoja, int fila, IReadOnlyList<string> encabezados)
    {
        for (var indice = 0; indice < encabezados.Count; indice++)
        {
            hoja.Cell(fila, indice + 1).SetValue(encabezados[indice]);
        }

        var rango = hoja.Range(fila, 1, fila, encabezados.Count);
        rango.Style
            .Font.SetBold()
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(ColorPrincipal))
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Alignment.SetWrapText();
        rango.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        rango.Style.Border.BottomBorderColor = XLColor.FromHtml(ColorPrincipal);
        hoja.Row(fila).Height = 30;
    }

    private static void EscribirEtiquetaValor(
        IXLWorksheet hoja,
        string direccionEtiqueta,
        string direccionValor,
        string etiqueta,
        object valor)
    {
        hoja.Cell(direccionEtiqueta).SetValue(etiqueta);
        hoja.Cell(direccionEtiqueta).Style.Font.SetBold().Font.SetFontColor(XLColor.DarkSlateGray);

        if (valor is DateTime fecha)
        {
            hoja.Cell(direccionValor).SetValue(fecha);
            hoja.Cell(direccionValor).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
        }
        else
        {
            EscribirTexto(hoja.Cell(direccionValor), valor.ToString());
        }
    }

    private static void EscribirTarjeta(
        IXLWorksheet hoja,
        string rangoEtiqueta,
        string rangoMonto,
        string etiqueta,
        decimal monto)
    {
        hoja.Range(rangoEtiqueta).Merge();
        hoja.Range(rangoMonto).Merge();
        hoja.Cell(rangoEtiqueta.Split(':')[0]).SetValue(etiqueta);
        hoja.Cell(rangoMonto.Split(':')[0]).SetValue(monto);

        var tarjeta = hoja.Range(
            hoja.Range(rangoEtiqueta).FirstCell().Address,
            hoja.Range(rangoMonto).LastCell().Address);
        tarjeta.Style.Fill.SetBackgroundColor(XLColor.FromHtml(ColorResumen));
        tarjeta.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tarjeta.Style.Border.OutsideBorderColor = XLColor.FromHtml(ColorBorde);
        tarjeta.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        tarjeta.FirstRow().Style.Font.SetFontColor(XLColor.DarkSlateGray);
        tarjeta.LastRow().Style.Font.SetBold().Font.SetFontSize(12);
        tarjeta.LastRow().Style.NumberFormat.Format = FormatoMoneda;
    }

    private static void EscribirTexto(IXLCell celda, string? valor)
    {
        celda.SetValue(string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim());
        celda.Style.NumberFormat.Format = "@";
    }

    private static void EscribirMonto(IXLCell celda, decimal monto)
    {
        celda.SetValue(monto);
        celda.Style.NumberFormat.Format = FormatoMoneda;
    }

    private static string TextoTipoParticipante(string? tipo) => tipo switch
    {
        "ContratacionPrevista" => "Contratación prevista",
        "Colaborador" => "Colaborador",
        _ => string.IsNullOrWhiteSpace(tipo) ? "—" : tipo.Trim()
    };

    private static string TextoAlcance(PeriodoResultadoForecastOpcion? periodo) => periodo is null
        ? "Todos los períodos"
        : $"{periodo.NumeroOrden}. {periodo.TipoPeriodo} - {periodo.FechaInicio:dd/MM/yyyy} al {periodo.FechaFin:dd/MM/yyyy}";
}
