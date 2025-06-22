using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventoTickets.Shared
{
    public enum StatusTicket
    {
        EmAberto,
        Entregue,
        Devolvido,
        Avulso
    }
    
    public enum StatusConexao
    {
        Conectado,
        Desconectado,
        Reconectando
    }
}
