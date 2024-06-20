using Internship_Backend_cpt.Models.ComparerModels;

namespace Internship_Backend_cpt.Models.DbModels
{
    public class TableModel
    {
        public int? TableId { get; set; }
        public string TableName { get; set; } = null!;
        public List<ColumnModel> ColumnModels { get; }
        public List<IndexModel> IndexModels { get; }
        public List<ConstraintModel> ConstraintModels { get; }
        public List<RelationModel> RelationModels { get; }

        public TableModel() {

            ColumnModels = [];
            IndexModels = [];
            ConstraintModels = [];
            RelationModels = [];
        }

    }
}