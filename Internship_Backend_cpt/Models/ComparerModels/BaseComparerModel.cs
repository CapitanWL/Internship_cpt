using Internship_Backend_cpt.Enums;

namespace Internship_Backend_cpt.Models.ComparerModels
{
    public abstract class BaseComparerModel
    {
        public bool HasChanges { get; set; }

        public ChangeTypesEnum ChangesType { get; set; }
    }
}
