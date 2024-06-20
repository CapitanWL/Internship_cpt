using Internship_Backend_cpt.Models.ComparerModels;

namespace Internship_Backend_cpt.Models.DbModels
{
    public class SchemaModel : BaseComparerModel
    {
        public string SchemaName { get; set; } = null!;
        public List<TableModel> TableModels { get; set; }
        public SchemaModel() {

            TableModels = [];
        }
    }
}
