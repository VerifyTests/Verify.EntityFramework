class DescriptiveModificationCommand(in ModificationCommandParameters parameters) :
    ModificationCommand(parameters)
{
    protected override IColumnModification CreateColumnModification(in ColumnModificationParameters parameters)
    {
        if (parameters.GenerateParameterName?.Target is not DescriptiveParameterNameGenerator generator)
        {
            return base.CreateColumnModification(parameters);
        }

        return CreateDescriptiveColumnModification(parameters, generator);
    }

    IColumnModification CreateDescriptiveColumnModification(in ColumnModificationParameters parameters, DescriptiveParameterNameGenerator generator)
    {
        var columnName = parameters.ColumnName;
        var entityName = parameters.Entry?.EntityType.ClrType.Name ?? "";
        var original = parameters.GenerateParameterName!;

        var modified = parameters with
        {
            GenerateParameterName = () =>
            {
                generator.SetColumnHint(entityName, columnName);
                return original();
            }
        };

        return base.CreateColumnModification(modified);
    }
}
