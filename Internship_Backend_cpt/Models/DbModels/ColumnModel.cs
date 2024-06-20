using Internship_Backend_cpt.Models.ComparerModels;

namespace Internship_Backend_cpt.Models.DbModels
{
    public class ColumnModel : BaseComparerModel
    {
        public string ColumnName { get; set; } = null!;

        public string ColumnType { get; set; } = null!;

        public int? ColumnSize { get; set; }

        public int ConstraintId { get; set; }

        public ConstraintModel Constraint { get; set; } = null!;

        public ColumnModel() { }
    }
}