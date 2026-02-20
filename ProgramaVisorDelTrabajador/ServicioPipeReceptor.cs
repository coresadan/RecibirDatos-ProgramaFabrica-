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

        public event EventHandler<string>? MensajeRecibido;

        public async Task IniciarEscuchaAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        NombrePipe, 
                        PipeDirection.In, 
                        1, 
                        PipeTransmissionMode.Byte, 
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(ct);

                    using var reader = new StreamReader(server);

                    string? contenido = await reader.ReadLineAsync();

                    if (!string.IsNullOrEmpty(contenido))
                    {
                        MensajeRecibido?.Invoke(this, contenido);
                    }
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