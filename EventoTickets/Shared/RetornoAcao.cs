using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventoTickets.Shared
{
    public class RetornoAcao
    {
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; }
        public Exception Exception { get; set; }
    }

    public class RetornoAcao<T> : RetornoAcao
    {
        public T Result { get; set; }
    }
}
