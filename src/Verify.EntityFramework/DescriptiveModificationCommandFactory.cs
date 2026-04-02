class DescriptiveModificationCommandFactory :
    IModificationCommandFactory
{
    public IModificationCommand CreateModificationCommand(
        in ModificationCommandParameters modificationCommandParameters) =>
        new DescriptiveModificationCommand(modificationCommandParameters);

    public INonTrackedModificationCommand CreateNonTrackedModificationCommand(
        in NonTrackedModificationCommandParameters modificationCommandParameters) =>
        new ModificationCommand(modificationCommandParameters);
}
