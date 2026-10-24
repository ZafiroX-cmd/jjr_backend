using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

namespace JJR.Api.Models;

public partial class JjrDbContext : DbContext
{
    public JjrDbContext()
    {
    }

    public JjrDbContext(DbContextOptions<JjrDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdelantosDescuento> AdelantosDescuentos { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<ComprobantesPago> ComprobantesPagos { get; set; }

    public virtual DbSet<Conductore> Conductores { get; set; }

    public virtual DbSet<ContratosLaborale> ContratosLaborales { get; set; }

    public virtual DbSet<CuentasEmpresa> CuentasEmpresas { get; set; }

    public virtual DbSet<DiasFestivo> DiasFestivos { get; set; }

    public virtual DbSet<EgresosOperativo> EgresosOperativos { get; set; }

    public virtual DbSet<Encomienda> Encomiendas { get; set; }

    public virtual DbSet<ItemsComprobante> ItemsComprobantes { get; set; }

    public virtual DbSet<LiquidacionesSocio> LiquidacionesSocios { get; set; }

    public virtual DbSet<MovimientosCaja> MovimientosCajas { get; set; }

    public virtual DbSet<Oficina> Oficinas { get; set; }

    public virtual DbSet<PagosNomina> PagosNominas { get; set; }

    public virtual DbSet<Pasaje> Pasajes { get; set; }

    public virtual DbSet<PredichosDemandum> PredichosDemanda { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Ruta> Rutas { get; set; }

    public virtual DbSet<RutaParada> RutaParadas { get; set; }

    public virtual DbSet<SeriesComprobante> SeriesComprobantes { get; set; }

    public virtual DbSet<Socio> Socios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<Vehiculo> Vehiculos { get; set; }

    public virtual DbSet<Viaje> Viajes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("estado_pago_enum", new[] { "PENDIENTE", "PAGADO", "ANULADO" })
            .HasPostgresEnum("modalidad_pago_enum", new[] { "FIJO_MENSUAL", "POR_VIAJE", "MIXTO" })
            .HasPostgresEnum("pagado_por_enum", new[] { "EMPRESA_ADMINISTRACION", "SOCIO_PROPIETARIO" })
            .HasPostgresEnum("tipo_documento_enum", new[] { "DNI", "RUC", "CE", "PASAPORTE" })
            .HasPostgresEnum("tipo_empleado_enum", new[] { "COUNTER", "CONDUCTOR", "ADMINISTRATIVO" })
            .HasPostgresEnum("tipo_persona_enum", new[] { "NATURAL", "JURIDICA" })
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.HasPostgresEnum<TipoMovimientoCaja>(null, "tipo_movimiento_enum", new NpgsqlNullNameTranslator());

        modelBuilder.Entity<AdelantosDescuento>(entity =>
        {
            entity.HasKey(e => e.MovimientoId).HasName("adelantos_descuentos_pkey");

            entity.Property(e => e.MovimientoId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.ProcesadoEnNomina).HasDefaultValue(false);

            entity.HasOne(d => d.Contrato).WithMany(p => p.AdelantosDescuentos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("adelantos_descuentos_contrato_id_fkey");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.ClienteId).HasName("clientes_pkey");

            entity.Property(e => e.ClienteId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.ActualizadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ComprobantesPago>(entity =>
        {
            entity.HasKey(e => e.ComprobanteId).HasName("comprobantes_pago_pkey");

            entity.Property(e => e.ComprobanteId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.ClienteDireccion).HasDefaultValueSql("'-'::character varying");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.EstadoSunat).HasDefaultValueSql("'Pendiente'::character varying");
            entity.Property(e => e.FechaDeEmision).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.HoraDeEmision).HasDefaultValueSql("CURRENT_TIME");
            entity.Property(e => e.Moneda).HasDefaultValueSql("'PEN'::character varying");
            entity.Property(e => e.MontoGravado).HasDefaultValueSql("0.00");
            entity.Property(e => e.MontoIgv).HasDefaultValueSql("0.00");
            entity.Property(e => e.TipoOperacion).HasDefaultValueSql("'0101'::character varying");

            entity.HasOne(d => d.Cliente).WithMany(p => p.ComprobantesPagos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("comprobantes_pago_cliente_id_fkey");

            entity.HasOne(d => d.Encomienda).WithMany(p => p.ComprobantesPagos).HasConstraintName("comprobantes_pago_encomienda_id_fkey");

            entity.HasOne(d => d.Pasaje).WithMany(p => p.ComprobantesPagos).HasConstraintName("comprobantes_pago_pasaje_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ComprobantesPagos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("comprobantes_pago_usuario_id_fkey");
        });

        modelBuilder.Entity<Conductore>(entity =>
        {
            entity.HasKey(e => e.ConductorId).HasName("conductores_pkey");

            entity.Property(e => e.ConductorId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Conductore).HasConstraintName("conductores_usuario_id_fkey");
        });

        modelBuilder.Entity<ContratosLaborale>(entity =>
        {
            entity.HasKey(e => e.ContratoId).HasName("contratos_laborales_pkey");

            entity.Property(e => e.ContratoId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PagoPorViaje).HasDefaultValueSql("0.00");

            entity.HasOne(d => d.Conductor).WithMany(p => p.ContratosLaborales).HasConstraintName("contratos_laborales_conductor_id_fkey");

            entity.HasOne(d => d.Socio).WithMany(p => p.ContratosLaborales).HasConstraintName("contratos_laborales_socio_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ContratosLaborales).HasConstraintName("contratos_laborales_usuario_id_fkey");
        });

        modelBuilder.Entity<CuentasEmpresa>(entity =>
        {
            entity.HasKey(e => e.CuentaId).HasName("cuentas_empresa_pkey");

            entity.Property(e => e.CuentaId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Moneda).HasDefaultValueSql("'PEN'::character varying");
            entity.Property(e => e.SaldoActual).HasDefaultValueSql("0.00");
        });

        modelBuilder.Entity<DiasFestivo>(entity =>
        {
            entity.HasKey(e => e.Fecha).HasName("dias_festivos_pkey");

            entity.Property(e => e.EsAltaDemanda).HasDefaultValue(true);
        });

        modelBuilder.Entity<EgresosOperativo>(entity =>
        {
            entity.HasKey(e => e.EgresoId).HasName("egresos_operativos_pkey");

            entity.Property(e => e.EgresoId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ValidadoPorAdmin).HasDefaultValue(false);

            entity.HasOne(d => d.RegistradoPorUsuario).WithMany(p => p.EgresosOperativos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("egresos_operativos_registrado_por_usuario_id_fkey");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.EgresosOperativos).HasConstraintName("egresos_operativos_vehiculo_id_fkey");

            entity.HasOne(d => d.Viaje).WithMany(p => p.EgresosOperativos).HasConstraintName("egresos_operativos_viaje_id_fkey");
        });

        modelBuilder.Entity<Encomienda>(entity =>
        {
            entity.HasKey(e => e.EncomiendaId).HasName("encomiendas_pkey");

            entity.Property(e => e.EncomiendaId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.ConfirmadoPorConductor).HasDefaultValue(false);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Estado).HasDefaultValueSql("'Recibido'::character varying");

            entity.HasOne(d => d.Destinatario).WithMany(p => p.EncomiendaDestinatarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("encomiendas_destinatario_id_fkey");

            entity.HasOne(d => d.RegistradoPorUsuario).WithMany(p => p.Encomienda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("encomiendas_registrado_por_usuario_id_fkey");

            entity.HasOne(d => d.Remitente).WithMany(p => p.EncomiendaRemitentes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("encomiendas_remitente_id_fkey");

            entity.HasOne(d => d.Viaje).WithMany(p => p.Encomienda).HasConstraintName("encomiendas_viaje_id_fkey");
        });

        modelBuilder.Entity<ItemsComprobante>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("items_comprobante_pkey");

            entity.Property(e => e.ItemId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.CodigoTipoAfectacionIgv).HasDefaultValueSql("'10'::character varying");
            entity.Property(e => e.NombreTributo).HasDefaultValueSql("'IGV'::character varying");
            entity.Property(e => e.PorcentajeIgv).HasDefaultValueSql("18.00");
            entity.Property(e => e.UnidadDeMedida).HasDefaultValueSql("'ZZ'::character varying");

            entity.HasOne(d => d.Comprobante).WithMany(p => p.ItemsComprobantes).HasConstraintName("items_comprobante_comprobante_id_fkey");
        });

        modelBuilder.Entity<LiquidacionesSocio>(entity =>
        {
            entity.HasKey(e => e.LiquidacionId).HasName("liquidaciones_socios_pkey");

            entity.Property(e => e.LiquidacionId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.EstadoPago).HasDefaultValueSql("'Pendiente'::character varying");
            entity.Property(e => e.MontoEgresosDeducibles).HasDefaultValueSql("0.00");
            entity.Property(e => e.MontoIngresoEncomiendas).HasDefaultValueSql("0.00");
            entity.Property(e => e.MontoIngresoPasajes).HasDefaultValueSql("0.00");
            entity.Property(e => e.ProcesadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Socio).WithMany(p => p.LiquidacionesSocios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("liquidaciones_socios_socio_id_fkey");

            entity.HasOne(d => d.Viaje).WithOne(p => p.LiquidacionesSocio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("liquidaciones_socios_viaje_id_fkey");
        });

        modelBuilder.Entity<MovimientosCaja>(entity =>
        {
            entity.HasKey(e => e.MovimientoId).HasName("movimientos_caja_pkey");

            entity.Property(e => e.MovimientoId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.FechaMovimiento).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Cuenta).WithMany(p => p.MovimientosCajas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movimientos_caja_cuenta_id_fkey");

            entity.HasOne(d => d.PagoNomina).WithMany(p => p.MovimientosCajas).HasConstraintName("movimientos_caja_pago_nomina_id_fkey");

            entity.HasOne(d => d.Socio).WithMany(p => p.MovimientosCajas).HasConstraintName("movimientos_caja_socio_id_fkey");

            entity.HasOne(d => d.UsuarioRegistro).WithMany(p => p.MovimientosCajas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movimientos_caja_usuario_registro_id_fkey");
        });

        modelBuilder.Entity<Oficina>(entity =>
        {
            entity.HasKey(e => e.OficinaId).HasName("oficinas_pkey");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<PagosNomina>(entity =>
        {
            entity.HasKey(e => e.PagoId).HasName("pagos_nomina_pkey");

            entity.Property(e => e.PagoId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TotalAdelantos).HasDefaultValueSql("0.00");
            entity.Property(e => e.TotalDescuentos).HasDefaultValueSql("0.00");

            entity.HasOne(d => d.Contrato).WithMany(p => p.PagosNominas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pagos_nomina_contrato_id_fkey");

            entity.HasOne(d => d.EjecutadoPorUsuario).WithMany(p => p.PagosNominas).HasConstraintName("pagos_nomina_ejecutado_por_usuario_id_fkey");
        });

        modelBuilder.Entity<Pasaje>(entity =>
        {
            entity.HasKey(e => e.PasajeId).HasName("pasajes_pkey");

            entity.Property(e => e.PasajeId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.ComprobanteEmitido).HasDefaultValue(false);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Estado).HasDefaultValueSql("'Reservado'::character varying");

            entity.HasOne(d => d.ClienteFacturacion).WithMany(p => p.PasajeClienteFacturacions).HasConstraintName("pasajes_cliente_facturacion_id_fkey");

            entity.HasOne(d => d.ClientePasajero).WithMany(p => p.PasajeClientePasajeros)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pasajes_cliente_pasajero_id_fkey");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.Pasajes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pasajes_creado_por_usuario_id_fkey");

            entity.HasOne(d => d.Viaje).WithMany(p => p.Pasajes).HasConstraintName("pasajes_viaje_id_fkey");

            entity.HasOne(d => d.OficinaEmision).WithMany(p => p.PasajesEmitidos)
                .HasConstraintName("pasajes_oficina_emision_id_fkey");
        });

        modelBuilder.Entity<PredichosDemandum>(entity =>
        {
            entity.HasKey(e => e.PrediccionId).HasName("predichos_demanda_pkey");

            entity.Property(e => e.PrediccionId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.GeneradoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Ruta).WithMany(p => p.PredichosDemanda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("predichos_demanda_ruta_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RolId).HasName("roles_pkey");
        });

        modelBuilder.Entity<Ruta>(entity =>
        {
            entity.HasKey(e => e.RutaId).HasName("rutas_pkey");

            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<RutaParada>(entity =>
        {
            entity.HasKey(e => e.ParadaId).HasName("ruta_paradas_pkey");

            entity.HasIndex(e => new { e.RutaId, e.Orden }, "uq_ruta_orden").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.EsOficina).HasDefaultValue(false);

            entity.HasOne(d => d.Ruta).WithMany(p => p.Paradas)
                .HasForeignKey(d => d.RutaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("ruta_paradas_ruta_id_fkey");
        });

        modelBuilder.Entity<SeriesComprobante>(entity =>
        {
            entity.HasKey(e => e.SerieId).HasName("series_comprobantes_pkey");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.UltimoNumero).HasDefaultValue(0);
        });

        modelBuilder.Entity<Socio>(entity =>
        {
            entity.HasKey(e => e.SocioId).HasName("socios_pkey");

            entity.Property(e => e.SocioId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.PorcentajeGanancia).HasDefaultValueSql("70.00");

            entity.HasOne(d => d.Usuario).WithOne(p => p.Socio).HasConstraintName("socios_usuario_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.UsuarioId).HasName("usuarios_pkey");

            entity.Property(e => e.UsuarioId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuarios_rol_id_fkey");

            entity.HasOne(d => d.Oficina).WithMany(p => p.Usuarios)
                .HasConstraintName("usuarios_oficina_id_fkey");
        });

        modelBuilder.Entity<Vehiculo>(entity =>
        {
            entity.HasKey(e => e.VehiculoId).HasName("vehiculos_pkey");

            entity.Property(e => e.VehiculoId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Estado).HasDefaultValueSql("'Disponible'::character varying");

            entity.HasOne(d => d.Socio).WithMany(p => p.Vehiculos).HasConstraintName("vehiculos_socio_id_fkey");
        });

        modelBuilder.Entity<Viaje>(entity =>
        {
            entity.HasKey(e => e.ViajeId).HasName("viajes_pkey");

            entity.Property(e => e.ViajeId).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Estado).HasDefaultValueSql("'Programado'::character varying");
            entity.Property(e => e.ManifiestoCerrado).HasDefaultValue(false);

            entity.HasOne(d => d.Conductor).WithMany(p => p.Viajes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("viajes_conductor_id_fkey");

            entity.HasOne(d => d.Ruta).WithMany(p => p.Viajes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("viajes_ruta_id_fkey");

            entity.HasOne(d => d.Vehiculo).WithMany(p => p.Viajes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("viajes_vehiculo_id_fkey");

            entity.HasOne(d => d.OficinaOrigen).WithMany(p => p.ViajesOrigen)
                .HasConstraintName("viajes_oficina_origen_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}