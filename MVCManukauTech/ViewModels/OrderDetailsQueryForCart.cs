using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MVCManukauTech.ViewModels
{
    public class OrderDetailsQueryForCart
    {
        public int OrderId { get; set; }
        [Key]
        public int LineNumber { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageFileName { get; set; }
        public Nullable<int> Quantity { get; set; }
        public Nullable<decimal> UnitCost { get; set; }
    }
}
