using ProgramaVisorDelTrabajador;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
using static ProgramaVisorDelTrabajador.CaracteristicasDePiezas;

namespace ProgramaVisorDelTrabajador
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ConfiguracionLogs.Inicializar();

            var receptor = new ServicioPipeReceptor();
            receptor.PiezaRecibida += (s, pieza) => Dispatcher.Invoke(() =>
            {
                if (pieza == null)
                {
                    FinalizacionLista();
                }
                else
                {
                    DataContext = pieza;
                    btnTerminar.IsEnabled = true;
                    btnIncidencia.IsEnabled = true;
                }
            });
            _ = receptor.IniciarEscuchaAsync(CancellationToken.None);
        }

        public void FinalizacionLista()
        {
            this.DataContext = null;
            btnTerminar.IsEnabled = false;
            btnIncidencia.IsEnabled = false;

            MessageBox.Show("Has finalizado el listado de piezas. En breves se te asignará la siguiente", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Warning("🚨 **ATENCIÓN TÉCNICO** 🚨\n\n" +
                "El trabajador ha **FINALIZADO** la lista de piezas en Santos.\n" +
                "Puesto de control: **Visor de Producción**.\n" +
                "Estado: **PENDIENTE DE NUEVA CARGA**. ⏳");
        }

        private async void btnTerminarClick(object sender, RoutedEventArgs e)
        {
            await new ServicioPipeEmisor().EnviarRespuestaOficinaAsync("acabada");
            this.DataContext = null;
        }

        private async void btnIncidenciaClick(object sender, RoutedEventArgs e)
        {
            await new ServicioPipeEmisor().EnviarRespuestaOficinaAsync("falta");
            this.DataContext = null;
        }
        private async void BtnSincronizarClick(object sender, RoutedEventArgs e)
        {

            await new ServicioPipeEmisor().EnviarRespuestaOficinaAsync("SOLICITAR_PIEZA_ACTUAL");
            Log.Information("PIPE El trabajador ha solicitado sincronización manual.");
        }

        private async void BtnCancelarListaClick(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("¿Quieres cancelar la lista actual?", "Santos - Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                await new ServicioPipeEmisor().EnviarRespuestaOficinaAsync("LISTA_CANCELADA_POR_TRABAJADOR");
                this.DataContext = null;
                Log.Warning("Lista cancelada por el operario.");
            }
        }
    }
}