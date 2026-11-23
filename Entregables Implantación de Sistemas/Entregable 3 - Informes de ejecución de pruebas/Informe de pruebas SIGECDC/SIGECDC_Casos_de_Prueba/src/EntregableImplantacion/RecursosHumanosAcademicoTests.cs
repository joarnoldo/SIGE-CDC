using Microsoft.EntityFrameworkCore;
using SIGECDC.Application.RecursosHumanos;
using SIGECDC.Tests.EntregableImplantacion.Soporte;

namespace SIGECDC.Tests.EntregableImplantacion;

[Collection(ColeccionAcademica.Nombre), Trait("Suite", "EntregableImplantacion")]
public sealed class RecursosHumanosAcademicoTests(ServidorAcademico servidor)
{
    [Fact(DisplayName = "RH-001 — Registrar y consultar colaborador"), Trait("HU", "HU-RH-001")]
    public async Task Rh001_RegistrarYConsultarColaborador()
    {
        await using var e = await servidor.NuevoAsync(); var datos = await e.DatosColaboradorAsync();
        var s = e.Obtener<IColaboradorService>(); var id = await s.RegistrarColaboradorAsync(datos, EntornoAcademico.Rrhh);
        var fila = Assert.Single(await s.ObtenerColaboradoresAsync("ACA-01")); Assert.Equal(id, fila.IdColaborador);
        var detalle = Assert.IsType<ColaboradorDetalle>(await s.ObtenerColaboradorPorIdAsync(id));
        Assert.Equal(datos.Identificacion, detalle.Identificacion); Assert.Equal("Activo", detalle.EstadoLaboral);
        await using var lectura = e.LecturaNueva(); Assert.Equal(datos.CorreoElectronico, (await lectura.Colaboradores.SingleAsync(c => c.IdColaborador == id)).CorreoElectronico);
    }

    [Fact(DisplayName = "RH-002 — Actualizar colaborador"), Trait("HU", "HU-RH-001")]
    public async Task Rh002_ActualizarDatosDeColaborador()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ColaboradorAsync(); var datos = await e.DatosColaboradorAsync();
        datos.CorreoElectronico = "actualizado@example.invalid"; datos.Telefono = "11111111";
        await e.Obtener<IColaboradorService>().ActualizarColaboradorAsync(id, datos, EntornoAcademico.Rrhh);
        await using var lectura = e.LecturaNueva(); var guardado = await lectura.Colaboradores.SingleAsync(c => c.IdColaborador == id);
        Assert.Equal("actualizado@example.invalid", guardado.CorreoElectronico); Assert.Equal("11111111", guardado.Telefono);
        Assert.Equal(datos.Identificacion, guardado.Identificacion); Assert.Equal(datos.CodigoColaborador, guardado.CodigoColaborador);
        Assert.Single(await lectura.Colaboradores.ToListAsync());
    }

    [Fact(DisplayName = "RH-003 — Desactivar conservando historial"), Trait("HU", "HU-RH-001")]
    public async Task Rh003_DesactivarColaboradorSinEliminarHistorial()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ColaboradorAsync();
        var historialAntes = await e.Db.BitacoraAuditoria.Where(b => b.Entidad == "Colaborador" && b.IdRegistro == id.ToString()).Select(b => b.IdBitacoraAuditoria).ToListAsync();
        Assert.NotEmpty(historialAntes);
        await e.Obtener<IColaboradorService>().DesactivarColaboradorAsync(id, EntornoAcademico.Rrhh);
        await using var lectura = e.LecturaNueva(); var guardado = await lectura.Colaboradores.Include(c => c.EstadoLaboral).SingleAsync(c => c.IdColaborador == id);
        Assert.NotNull(guardado.EstadoLaboral);
        Assert.Equal("Inactivo", guardado.EstadoLaboral.Nombre);
        Assert.NotNull(await e.Obtener<IColaboradorService>().ObtenerColaboradorPorIdAsync(id));
        foreach (var registro in historialAntes) Assert.True(await lectura.BitacoraAuditoria.AnyAsync(b => b.IdBitacoraAuditoria == registro));
        Assert.True(await lectura.BitacoraAuditoria.AnyAsync(b => b.Entidad == "Colaborador" && b.IdRegistro == id.ToString() && b.Accion.Contains("DESACTIV")));
    }

    [Fact(DisplayName = "RH-004 — Catálogos y asignación organizativa"), Trait("HU", "HU-RH-002")]
    public async Task Rh004_CrearPuestoDepartamentoYAsignarColaborador()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ColaboradorAsync();
        var departamentos = e.Obtener<IDepartamentoService>(); var puestos = e.Obtener<IPuestoService>();
        await departamentos.RegistrarAsync(new() { Nombre = "Departamento nuevo QA" });
        var departamento = (await departamentos.ObtenerTodosAsync()).Single(d => d.Nombre == "Departamento nuevo QA");
        await puestos.RegistrarAsync(new() { Nombre = "Puesto nuevo QA", IdDepartamento = departamento.IdDepartamento });
        var puesto = (await puestos.ObtenerTodosAsync()).Single(p => p.Nombre == "Puesto nuevo QA");
        await e.Obtener<IColaboradorService>().AsignarPuestoYDepartamentoAsync(new() { IdColaborador = id, IdPuesto = puesto.IdPuesto, IdDepartamento = departamento.IdDepartamento }, EntornoAcademico.Rrhh);
        var detalle = Assert.IsType<ColaboradorDetalle>(await e.Obtener<IColaboradorService>().ObtenerColaboradorPorIdAsync(id));
        Assert.Equal("Puesto nuevo QA", detalle.Puesto); Assert.Equal("Departamento nuevo QA", detalle.Departamento);
        await using var lectura = e.LecturaNueva(); Assert.Equal(puesto.IdPuesto, (await lectura.Colaboradores.SingleAsync(c => c.IdColaborador == id)).IdPuesto);
    }

    [Fact(DisplayName = "RH-005 — Adjuntar y recuperar documento válido"), Trait("HU", "HU-RH-003")]
    public async Task Rh005_AdjuntarDocumentoAlExpedienteCorrecto()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ColaboradorAsync(); var otro = await e.ColaboradorAsync("ACA-02");
        var s = e.Obtener<IContratoDocumentoService>(); var contenido = EntornoAcademico.PdfSintetico();
        using var archivo = new SolicitudArchivoDocumento { IdColaborador = id, IdTipoDocumento = (await s.ObtenerTiposDocumentoAsync()).First().Id,
            NombreOriginal = "contrato-academico.pdf", MimeType = "application/pdf", TamanoBytes = contenido.Length, Contenido = new MemoryStream(contenido) };
        var documento = await s.AdjuntarDocumentoAsync(archivo, EntornoAcademico.Rrhh);
        Assert.Equal(documento, Assert.Single((await s.ObtenerExpedienteDocumentalAsync(id)).Documentos).IdDocumentoArchivo);
        Assert.Empty((await s.ObtenerExpedienteDocumentalAsync(otro)).Documentos);
        var descarga = await s.AbrirDocumentoAsync(documento); Assert.NotNull(descarga);
        using var bytes = new MemoryStream(); await descarga.Contenido.CopyToAsync(bytes); await descarga.Contenido.DisposeAsync();
        Assert.Equal(contenido, bytes.ToArray()); Assert.Equal("application/pdf", descarga.MimeType); Assert.Equal("contrato-academico.pdf", descarga.NombreOriginal);
    }

    [Fact(DisplayName = "RH-006 — Rechazar documento con contenido inválido"), Trait("HU", "HU-RH-003")]
    public async Task Rh006_RechazarArchivoQueNoEsPdf()
    {
        await using var e = await servidor.NuevoAsync(); var id = await e.ColaboradorAsync(); var s = e.Obtener<IContratoDocumentoService>();
        var contenido = System.Text.Encoding.UTF8.GetBytes("Este texto no es un documento PDF");
        using var archivo = new SolicitudArchivoDocumento { IdColaborador = id, IdTipoDocumento = (await s.ObtenerTiposDocumentoAsync()).First().Id,
            NombreOriginal = "falso.pdf", MimeType = "application/pdf", TamanoBytes = contenido.Length, Contenido = new MemoryStream(contenido) };
        var error = await Assert.ThrowsAsync<ArgumentException>(() => s.AdjuntarDocumentoAsync(archivo, EntornoAcademico.Rrhh));
        Assert.False(string.IsNullOrWhiteSpace(error.Message)); Assert.Empty((await s.ObtenerExpedienteDocumentalAsync(id)).Documentos);
        Assert.Empty(Directory.Exists(e.Archivos) ? Directory.GetFiles(e.Archivos, "*", SearchOption.AllDirectories) : []);
    }
}
