using ProgramaVisorDelTrabajador;
using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace ProgramaVisorDelTrabajador
{
    public class ServicioPipeEmisor
    {
        private const string NombrePipeRespuesta = "PipeSantosRespuesta";

        public async Task EnviarRespuestaOficinaAsync(string respuesta)
        {
            try
            {

                using var client = new NamedPipeClientStream(".", NombrePipeRespuesta, PipeDirection.Out);
                await client.ConnectAsync(2000);

                using var writer = new StreamWriter(client, Encoding.UTF8);

                await writer.WriteLineAsync(respuesta);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enviando a oficina: {ex.Message}");
            }
        }
    }
}