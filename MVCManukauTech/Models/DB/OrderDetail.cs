using System;
using System.Collections.Generic;

namespace MVCManukauTech.Models.DB
{
    public partial class OrderDetail
    {
        public int OrderId { get; set; }
        public int LineNumber { get; set; }
        public string ProductId { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitCost { get; set; }

        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }
}
