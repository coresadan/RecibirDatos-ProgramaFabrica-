using ProgramaVisorDelTrabajador;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProgramaVisorDelTrabajador
{
    public class ServicioPipeReceptor
    {
        private const string NombrePipe = "PipeSantosPiezas";

        public event EventHandler<CaracteristicasDePiezas>? PiezaRecibida;

        public async Task IniciarEscuchaAsync(CancellationToken ct)
        {
            var opcionesJson = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(NombrePipe, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(ct);

                    using var reader = new StreamReader(server);
                    string? json = await reader.ReadLineAsync();

                    CaracteristicasDePiezas? pieza = null;
                    if (!string.IsNullOrEmpty(json))
                    {
                        pieza = JsonSerializer.Deserialize<CaracteristicasDePiezas>(json, opcionesJson);
                    }
                    PiezaRecibida?.Invoke(this, pieza!);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception)
                {
                    await Task.Delay(1000, ct);
                }
            }
        }
    }
}
