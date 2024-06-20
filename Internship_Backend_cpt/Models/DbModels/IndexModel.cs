using Internship_Backend_cpt.Models.ComparerModels;

namespace Internship_Backend_cpt.Models.DbModels
{
    public class IndexModel : BaseComparerModel
    {
        public int IndexModelId { get; set; }
        public string IndexName { get; set; } = null!;
        public bool IsClustered { get; set; } // CLUSTERED or NONCLUSTERED
        public bool IsUnique { get; set; }

        public List<ColumnModel> ColumnModels { get; set; }

        public IndexModel() 
        {
            ColumnModels = [];
        }
    }
}