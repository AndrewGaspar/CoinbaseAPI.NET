using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class RecordsPage
    {
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("num_pages")]
        public int NumPages { get; set; }

        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }
    }
}
