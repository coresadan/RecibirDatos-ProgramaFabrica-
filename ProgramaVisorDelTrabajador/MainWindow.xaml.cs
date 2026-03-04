using ProgramaVisorDelTrabajador;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Data.Sqlite;
using static ProgramaVisorDelTrabajador.CaracteristicasDePiezas;

namespace ProgramaVisorDelTrabajador
{
    public partial class MainWindow : Window
    {
        public bool _estaTrabajando = false;

        private readonly ServicioPipeEmisor _emisor = new ServicioPipeEmisor();

        public MainWindow()
        {
            InitializeComponent();

            // 1. Estado inicial de seguridad
            ActualizarEstadoInterfaz(false);

            // 2. Logs y servicios
            ConfiguracionLogs.Inicializar();
            var receptor = new ServicioPipeReceptor();

            // 3. Configuración del receptor inteligente
            receptor.MensajeRecibido += (s, contenido) => Dispatcher.Invoke(async () =>
            {
                try
                {
                    // 1. Limpieza básica del mensaje
                    if (string.IsNullOrWhiteSpace(contenido)) return;
                    string mensaje = contenido.Trim();

                    // 2. --- COMANDOS DE CONTROL ---
                    if (mensaje == "SESION_INICIADA")
                    {
                        ActualizarEstadoInterfaz(true);
                        Log.Information("🔓 Acceso concedido por Oficina.");
                        return;
                    }

                    if (mensaje == "SESION_FINALIZADA")
                    {
                        ActualizarEstadoInterfaz(false);
                        Log.Warning("🔒 Acceso revocado por Oficina.");
                        return;
                    }

                    // 3. --- RECEPCIÓN DE PIEZAS (JSON) ---
                    if (mensaje.StartsWith("{"))
                    {
                        var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var piezaCargada = JsonSerializer.Deserialize<CaracteristicasDePiezas>(mensaje, opciones);

                        if (piezaCargada != null)
                        {
                            DataContext = piezaCargada;
                            ActualizarEstadoInterfaz(true);
                            Log.Information($"📦 Pieza {piezaCargada.Id} cargada en pantalla.");
                            return;
                        }
                    }

                    // 4. --- GESTIÓN DE FIN DE LISTA O PIEZA NULA ---
                    if (mensaje.ToLower() == "null" || mensaje == "FIN_LISTA")
                    {
                        Log.Information("🏁 La oficina indica que no hay más piezas pendientes.");
                        await _emisor.EnviarRespuestaOficinaAsync("LIBRE");
                        FinalizacionLista();
                    }
                    else
                    {
                        Log.Debug($"✉️ Mensaje ignorado o no reconocido: {mensaje}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"❌ Error en recepción de datos: {ex.Message}");
                }
            });

            // 4. Arrancamos la escucha del Pipe
            _ = receptor.IniciarEscuchaAsync(CancellationToken.None);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("🔄 Iniciando sincronización con la Oficina...");
        }

        private void ActualizarEstadoInterfaz(bool trabajando)
        {
            // 1. Guardamos el estado interno
            _estaTrabajando = trabajando;

            // 2. Bloqueamos o habilitamos los botones de acción
            btnTerminar.IsEnabled = trabajando;
            btnIncidencia.IsEnabled = trabajando;

            // 3. Gestionamos la limpieza de datos y los mensajes de estado
            if (!trabajando)
            {
                DataContext = null;
                lblEstado.Text = "🛑 SISTEMA BLOQUEADO - ESPERANDO LOGIN EN OFICINA";
                lblEstado.Foreground = System.Windows.Media.Brushes.Tomato; // Un color de aviso
            }
            else
            {
                lblEstado.Text = "🟢 SISTEMA LISTO - OPERARIO ACTIVO";
                lblEstado.Foreground = System.Windows.Media.Brushes.LightGreen; // Un color de "OK"
            }

            // 4. Opcional: Feedback visual de transparencia para todo el visor
            this.Opacity = trabajando ? 1.0 : 0.8;
        }

        public void FinalizacionLista()
        {
            ActualizarEstadoInterfaz(false);
            MessageBox.Show("Has finalizado el listado de piezas. Esperando nueva carga...",
                            "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnTerminarClick(object sender, RoutedEventArgs e)
        {
            // 1. Obtener la pieza que tenemos en pantalla
            var pieza = DataContext as CaracteristicasDePiezas;

            if (pieza != null)
            {
                // 2. ACTUALIZAR BASE DE DATOS (Lo que faltaba)
                ActualizarEstadoLocal(pieza.Id, "Terminado");
            }

            // 3. Avisar a la oficina como ya hacías
            await _emisor.EnviarRespuestaOficinaAsync("ACABADA");
            ActualizarEstadoInterfaz(false);
        }

        private void ActualizarEstadoLocal(int id, string estado)
        {
            try
            {
                using var conexion = new SqliteConnection("Data Source=C:\\pruebas\\BDPiezas.s3db");
                conexion.Open();
                var cmd = new SqliteCommand("UPDATE RegistroDePiezas SET Estado = @est WHERE Id = @id", conexion);
                cmd.Parameters.AddWithValue("@est", estado);
                cmd.Parameters.AddWithValue("@id", id);
                int filas = cmd.ExecuteNonQuery();

                if (filas > 0) Log.Information($"✅ DB local actualizada: Pieza {id} -> {estado}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error guardando en DB: {ex.Message}");
            }
        }

        private async void btnIncidenciaClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is CaracteristicasDePiezas pieza)
            {
                ActualizarEstadoLocal(pieza.Id, "Falta");

                Log.Information($"⚠️ Incidencia registrada localmente para pieza {pieza.Id}");

                await _emisor.EnviarRespuestaOficinaAsync($"INCIDENCIA|{pieza.Id}");

                ActualizarEstadoInterfaz(false);
            }
        }

        private async void BtnSincronizarClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult respuesta = MessageBox.Show("¿Deseas solicitar la pieza actual a la oficina?",
                                                         "Confirmar Sincronización",
                                                         MessageBoxButton.YesNo,
                                                         MessageBoxImage.Question);

            if (respuesta == MessageBoxResult.Yes)
            {
                try
                {
                    lblEstado.Text = "⏳ ESPERANDO RESPUESTA DE OFICINA...";
                    lblEstado.Foreground = System.Windows.Media.Brushes.Orange;

                    await SolicitarSiguientePieza();

                    await Task.Delay(2000);

                    if (!_estaTrabajando)
                    {
                        lblEstado.Text = "🛑 OFICINA NO RESPONDE (¿SIN SESIÓN?)";
                        lblEstado.Foreground = System.Windows.Media.Brushes.Tomato;

                        MessageBox.Show("La oficina no ha iniciado sesión con ningún usuario.\n\nVerifique que el programa de oficina disponga de conexión.",
                                        "Sin Respuesta", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error al sincronizar: {ex.Message}");
                }
            }
        }

        private async void BtnCancelarListaClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Cancelar lista?", "Aviso", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await _emisor.EnviarRespuestaOficinaAsync("LIBRE");
                ActualizarEstadoInterfaz(false);
            }
        }

        private async Task SolicitarSiguientePieza() =>
            await _emisor.EnviarRespuestaOficinaAsync("SOLICITAR_PIEZA_ACTUAL");
    }
}