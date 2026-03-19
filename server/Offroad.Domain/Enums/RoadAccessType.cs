using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Domain.Enums
{
    public enum RoadAccessType
    {
        Yes,
        No, // zakaz vjezdu pro vsechny bez dodatkove tabulky
        Private, // typicky oznaceno jako zakaz vjezdu motorovym vozidlum krome vozidel s povolenim pro obyvatele, sluzebni vozidla, atd
        Forestry,// typicky oznaceno jako zakaz vjezdu motorovym vozidlum krome vozidel s povolenim lesu cr atd
        Agricultural, // typicky oznaceno jako zakaz vjezdu motorovym vozidlum krome vozidel s povolenim pro zemedelecke prace atd
        Customers, // mimo zakazniky
        Destination, // zakaz prujezdu
        Delivery, // mimo zasobovani
        Unknown
    }
}
