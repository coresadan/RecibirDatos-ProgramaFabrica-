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
        // Centralizamos el emisor aquí
        private readonly ServicioPipeEmisor _emisor = new ServicioPipeEmisor();

        public MainWindow()
        {
            InitializeComponent();
            ConfiguracionLogs.Inicializar();

            var receptor = new ServicioPipeReceptor();

            receptor.MensajeRecibido += (s, contenido) => Dispatcher.Invoke(async () =>
            {
                try
                {
                    var opcionesJson = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var piezaCargada = JsonSerializer.Deserialize<CaracteristicasDePiezas>(contenido, opcionesJson);

                    if (piezaCargada == null)
                    {
                        // Usamos el emisor central
                        await _emisor.EnviarRespuestaOficinaAsync("LIBRE");
                        FinalizacionLista();
                    }
                    else
                    {
                        DataContext = piezaCargada;
                        ActualizarEstadoInterfaz(true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error al procesar datos: {ex.Message}");
                }
            });

            _ = receptor.IniciarEscuchaAsync(CancellationToken.None);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("🔄 Iniciando sincronización con la Oficina...");
            await _emisor.EnviarRespuestaOficinaAsync("SOLICITAR_PIEZA_ACTUAL");
        }

        private void ActualizarEstadoInterfaz(bool trabajando)
        {
            _estaTrabajando = trabajando;
            btnTerminar.IsEnabled = trabajando;
            btnIncidencia.IsEnabled = trabajando;
            if (!trabajando) DataContext = null;
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
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error($"Error guardando en DB desde fábrica: {ex.Message}");
            }
        }

        private async void btnIncidenciaClick(object sender, RoutedEventArgs e)
        {
            // 1. Obtener la pieza que tenemos en pantalla
            if (DataContext is CaracteristicasDePiezas pieza)
            {
                // 2. Guardamos el estado de error en la base de datos SQLite
                ActualizarEstadoLocal(pieza.Id, "FALTA/RECHAZO");
            }

            // 3. Avisamos a la oficina por el Pipe
            await _emisor.EnviarRespuestaOficinaAsync("FALTA");
            ActualizarEstadoInterfaz(false);
        }

        private async void BtnSincronizarClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult respuesta = MessageBox.Show("Solicitando sincronización con la Oficina... Continuar?", "Sincronización", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (respuesta == MessageBoxResult.Yes)
            {
                await SolicitarSiguientePieza();
            }
            if (respuesta == MessageBoxResult.No)
            {
                MessageBox.Show("Sincronización cancelada. ", "Sincronización", MessageBoxButton.OK, MessageBoxImage.Information);
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