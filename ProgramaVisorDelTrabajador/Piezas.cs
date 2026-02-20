using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProgramaVisorDelTrabajador
{
    public class CaracteristicasDePiezas : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _nombre = string.Empty;
        
        public string Nombre
        {
            get => _nombre;
            set { if (_nombre != value) { _nombre = value; OnPropertyChanged(); } }
        }

        private string _color = string.Empty;
        public string Color
        {
            get => _color;
            set { if (_color != value) { _color = value; OnPropertyChanged(); } }
        }

        private int _id;
        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private decimal _largo;
        public decimal Largo
        {
            get => _largo;
            set { if (_largo != value) { _largo = value; OnPropertyChanged(); } }
        }

        private decimal _ancho;
        public decimal Ancho
        {
            get => _ancho;
            set { if (_ancho != value) { _ancho = value; OnPropertyChanged(); } }
        }

        public DatosOficina Datos { get; set; } = new DatosOficina();
        public List<Fabricacion> Fabricaciones { get; set; } = new List<Fabricacion>();

        // Método estrella de sintetización
        public CaracteristicasDePiezas Clonar()
        {
            string json = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<CaracteristicasDePiezas>(json);
        }


        // --- CLASES INTERNAS ---
        public class DatosOficina
        {
            public bool EstaTerminada {  get; set; }
            public bool Falta { get; set; }
            public bool Error { get; set; }
        }

        public class Fabricacion
        {
            public DateTime Fecha { get; set; }
            public string Maquina { get; set; }
            public string EstadoDeLaPieza { get; set; }
            public string Operario { get; set; }
        }
    }
}
