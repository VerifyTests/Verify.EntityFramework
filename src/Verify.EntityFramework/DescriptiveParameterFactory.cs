class DescriptiveParameterFactory :
    IModificationCommandFactory,
    IParameterNameGeneratorFactory
{
    public IModificationCommand CreateModificationCommand(
        in ModificationCommandParameters modificationCommandParameters) =>
        new DescriptiveModificationCommand(modificationCommandParameters);

    public INonTrackedModificationCommand CreateNonTrackedModificationCommand(
        in NonTrackedModificationCommandParameters modificationCommandParameters) =>
        new ModificationCommand(modificationCommandParameters);

    public ParameterNameGenerator Create() => new DescriptiveParameterNameGenerator();
}
