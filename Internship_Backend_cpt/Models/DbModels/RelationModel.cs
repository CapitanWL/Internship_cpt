using Internship_Backend_cpt.Models.ComparerModels;

namespace Internship_Backend_cpt.Models.DbModels
{
    public class RelationModel : BaseComparerModel
    {
        public string RelationModelName { get; set; } = null!;
        public string RelationModelType { get; set; } = null!;
        public string TableModelToName { get; set; } = null!;
        public string TableModelFromName { get; set; } = null!;
        public string ColumnModelFromName { get; set; } = null!;
        public string ColumnModelToName { get; set; } = null!;
    }
}