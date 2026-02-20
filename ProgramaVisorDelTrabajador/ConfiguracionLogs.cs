using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProgramaVisorDelTrabajador
{
    public static class ConfiguracionLogs
    {
        public static void Inicializar()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Telegram("8572448307:AAEpWviIJ0qqd1YPBXysRjl2SpsXmUprVIw", "5688537233")
                .WriteTo.File("logs/log_produccion.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information("🚀 Sistema de Control Santos iniciado. Conexión establecida con el programa de Fabrica.");
        }

        public static void NotificarFinDeLista(int totalPiezas, int faltas)
        {
            string mensaje = $"🏁 **Santos - Fin de Lista**\n" +
                             $"✅ Todas las piezas procesadas.\n" +
                             $"❌ Incidencias detectadas: {faltas}\n" +
                             $"📦 Total de la carga: {totalPiezas}";

            Log.Information(mensaje);
        }
    }
}
