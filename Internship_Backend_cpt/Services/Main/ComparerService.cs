using Internship_Backend_cpt.Models.ComparerModels;
using Internship_Backend_cpt.Models.DbModels;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Internship_Backend_cpt.Services.Main
{
    public class ComparerService
    {
        public async Task<(SchemaModel, SchemaModel)>
            Compare(SchemaModel parentModel, SchemaModel childModel)
        {
            if (parentModel.SchemaName != childModel.SchemaName)
            {
                childModel.HasChanges = true;
                childModel.ChangesType = Enums.ChangeTypesEnum.Edited;
            }
            else
            {
                childModel.TableModels = await CompareTables(parentModel.TableModels, childModel.TableModels);
            }

            return (parentModel, childModel);
        }

        private async Task<List<TableModel>> CompareTables(List<TableModel> parentModel, List<TableModel> childModel)
        {
            foreach (var parent in parentModel)
            {
                foreach (var child in childModel)
                {
                    if (parent.TableName == child.TableName)
                    {
                        CompareRelations(parent, child);
                        CompareConstraints(parent, child);
                        CompareColumns(parent, child);
                    }
                }
            }

            return childModel;
        }

        #region compare indexes

        public void CompareIndexes(TableModel parent, TableModel child)
        {
            foreach (var childIndex in child.IndexModels)
            {
                var correspondingParentIndex = parent.IndexModels.FirstOrDefault(parentIndex => parentIndex.IndexName == childIndex.IndexName);
                if (correspondingParentIndex != null)
                {
                    if (!AreIndexesEqual(correspondingParentIndex, childIndex))
                    {
                        childIndex.HasChanges = true;
                        childIndex.ChangesType = Enums.ChangeTypesEnum.Edited;
                    }
                }
                else
                {
                    childIndex.HasChanges = true;
                    childIndex.ChangesType = Enums.ChangeTypesEnum.Added;
                }
            }

            foreach (var parentIndex in parent.IndexModels)
            {
                if (!child.IndexModels.Any(childIndex => childIndex.IndexName == parentIndex.IndexName))
                {
                    var deletedIndex = new IndexModel
                    {
                        IndexName = parentIndex.IndexName,
                        HasChanges = true,
                        ChangesType = Enums.ChangeTypesEnum.Delited
                    };
                    child.IndexModels.Add(deletedIndex);
                }
            }
        }

        private bool AreIndexesEqual(IndexModel index1, IndexModel index2)
        {
            return index1.IsClustered == index2.IsClustered
                && index1.IsUnique == index2.IsUnique
                && index1.ColumnModels.SequenceEqual(index2.ColumnModels);
        }

        #endregion


        #region compare constraints

        public void CompareConstraints(TableModel parent, TableModel child)
        {
            foreach (var childConstraint in child.ConstraintModels)
            {
                var matchingParentConstraint = parent.ConstraintModels.FirstOrDefault(parentConstraint =>
                parentConstraint.ConstraintName == childConstraint.ConstraintName);

                if (matchingParentConstraint != null)
                {
                    if (!AreConstraintsEqual(matchingParentConstraint, childConstraint))
                    {
                        childConstraint.HasChanges = true;
                        childConstraint.ChangesType = Enums.ChangeTypesEnum.Edited;
                    }
                }
                else
                {
                    childConstraint.HasChanges = true;
                    childConstraint.ChangesType = Enums.ChangeTypesEnum.Added;
                }
            }

            foreach (var parentConstraint in parent.ConstraintModels)
            {
                if (!child.ConstraintModels.Any(childConstraint => childConstraint.ConstraintName == parentConstraint.ConstraintName))
                {
                    var deletedConstraint = new ConstraintModel
                    {
                        ConstraintName = parentConstraint.ConstraintName,
                        HasChanges = true,
                        ChangesType = Enums.ChangeTypesEnum.Delited,
                        CheckedInfo = parentConstraint.CheckedInfo,
                        IsForeignKey = parentConstraint.IsForeignKey,
                        ColumnModels = parentConstraint.ColumnModels,
                        ConstraintType = parentConstraint.ConstraintType,
                        IsNullable = parentConstraint.IsNullable,
                        IsPrimaryKey = parentConstraint.IsPrimaryKey,
                        IsUnique = parentConstraint.IsUnique,
                        OnDeleteInfo = parentConstraint.OnDeleteInfo,
                        OnUpdateInfo = parentConstraint.OnUpdateInfo,
                    };
                    child.ConstraintModels.Add(deletedConstraint);
                }
            }
        }

        private bool AreConstraintsEqual(ConstraintModel constraint1, ConstraintModel constraint2)
        {
            return constraint1.ConstraintType == constraint2.ConstraintType
                && constraint1.IsUnique == constraint2.IsUnique
                && constraint1.IsNullable == constraint2.IsNullable
                && constraint1.IsPrimaryKey == constraint2.IsPrimaryKey
                && constraint1.CheckedInfo == constraint2.CheckedInfo
                && constraint1.IsForeignKey == constraint2.IsForeignKey
                && constraint1.OnDeleteInfo == constraint2.OnDeleteInfo
                && constraint1.OnUpdateInfo == constraint2.OnUpdateInfo;
        }

        #endregion


        #region compare relations

        public void CompareRelations(TableModel parent, TableModel child)
        {
            foreach (var childRelation in child.RelationModels)
            {
                var matchingParentRelation = parent.RelationModels.FirstOrDefault(parentRelation => parentRelation.RelationModelName == childRelation.RelationModelName);

                if (matchingParentRelation != null)
                {
                    if (!AreRelationsEqual(matchingParentRelation, childRelation))
                    {
                        childRelation.HasChanges = true;
                        childRelation.ChangesType = Enums.ChangeTypesEnum.Edited;
                    }
                }
                else
                {
                    childRelation.HasChanges = true;
                    childRelation.ChangesType = Enums.ChangeTypesEnum.Added;
                }
            }

            foreach (var parentRelation in parent.RelationModels)
            {
                if (!child.RelationModels.Any(childRelation => childRelation.RelationModelName == parentRelation.RelationModelName))
                {
                    var deletedRelation = new RelationModel
                    {
                        RelationModelName = parentRelation.RelationModelName,
                        HasChanges = true,
                        ChangesType = Enums.ChangeTypesEnum.Delited,
                        ColumnModelFromName = parentRelation.ColumnModelFromName,
                        RelationModelType = parentRelation.RelationModelType,
                        ColumnModelToName = parentRelation.ColumnModelToName,
                        TableModelFromName = parentRelation.TableModelFromName,
                        TableModelToName = parentRelation.TableModelToName,
                    };
                    child.RelationModels.Add(deletedRelation);
                }
            }
        }

        private bool AreRelationsEqual(RelationModel relation1, RelationModel relation2)
        {
            return relation1.RelationModelType == relation2.RelationModelType
                && relation1.ColumnModelFromName == relation2.ColumnModelFromName
                && relation1.ColumnModelToName == relation2.ColumnModelToName
                && relation1.TableModelFromName == relation2.TableModelFromName
                && relation1.TableModelToName == relation2.TableModelToName;
        }


        #endregion


        #region compare columns

        public void CompareColumns(TableModel parent, TableModel child)
        {
            foreach (var childColumn in child.ColumnModels)
            {
                var matchingParentColumn = parent.ColumnModels.FirstOrDefault(parentColumn => parentColumn.ColumnName == childColumn.ColumnName);

                if (matchingParentColumn != null)
                {
                    if (!AreColumnsEqual(matchingParentColumn, childColumn))
                    {
                        childColumn.HasChanges = true;
                        childColumn.ChangesType = Enums.ChangeTypesEnum.Edited;
                    }
                }
                else
                {
                    childColumn.HasChanges = true;
                    childColumn.ChangesType = Enums.ChangeTypesEnum.Added;
                }
            }

            foreach (var parentColumn in parent.ColumnModels)
            {
                if (!child.ColumnModels.Any(childColumn => childColumn.ColumnName == parentColumn.ColumnName))
                {
                    var deletedColumn = new ColumnModel
                    {
                        ColumnName = parentColumn.ColumnName,
                        HasChanges = true,
                        ChangesType = Enums.ChangeTypesEnum.Delited
                    };
                    child.ColumnModels.Add(deletedColumn);
                }
            }
        }

        private bool AreColumnsEqual(ColumnModel column1, ColumnModel column2)
        {
            return column1.ColumnSize == column2.ColumnSize
                && column1.ColumnType == column2.ColumnType
                && column1.Constraint == column2.Constraint;
        }

        #endregion

    }
}
