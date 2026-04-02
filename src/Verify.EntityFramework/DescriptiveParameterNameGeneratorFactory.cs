class DescriptiveParameterNameGeneratorFactory :
    IParameterNameGeneratorFactory
{
    public ParameterNameGenerator Create() => new DescriptiveParameterNameGenerator();
}
