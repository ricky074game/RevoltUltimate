using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevoltUltimate.API.Objects
{
    public class SearchResult
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Summary { get; set; }
        public Cover? Cover { get; set; }
        public string? AbsoluteCoverUrl => Cover?.Url != null ? $"https:{Cover.Url}" : null;
    }
}
