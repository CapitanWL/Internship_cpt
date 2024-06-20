using Internship_Backend_cpt.Models.ComparerModels;

namespace Internship_Backend_cpt.Models.DbModels
{
    public class ConstraintModel : BaseComparerModel
    {
        public string ConstraintName { get; set; } = null!;

        public string ConstraintType { get; set; } = null!;

        public bool? IsUnique { get; set; }

        public bool? IsNullable { get; set; }

        public bool? IsPrimaryKey { get; set; }

        public bool? IsForeignKey { get; set; }

        public string? CheckedInfo {  get; set; }

        public string? OnDeleteInfo { get; set; }

        public string? OnUpdateInfo { get; set; }

        public List<ColumnModel> ColumnModels { get; set; }

        public ConstraintModel() {
        
            ColumnModels = [];
        }
    }
}