using Domain.Model;
namespace Api.Banking.Mutation;

public class WrapperInputType : InputObjectType<Wrapper>
{
    protected override void Configure(IInputObjectTypeDescriptor<Wrapper> inputObjectTypeDescriptor)
    {
    }
}