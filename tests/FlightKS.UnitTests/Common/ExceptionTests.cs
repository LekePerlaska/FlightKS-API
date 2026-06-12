using FlightKS.Exceptions;

namespace FlightKS.UnitTests.Common;

public class ExceptionTests
{
    [Fact]
    public void NotFoundException_HasCorrectStatusAndCode()
    {
        var ex = new NotFoundException("Item not found");

        ex.Message.Should().Be("Item not found");
        ex.StatusCode.Should().Be(404);
        ex.Code.Should().Be("not_found");
    }

    [Fact]
    public void BusinessRuleException_HasCorrectStatusAndCode()
    {
        var ex = new BusinessRuleException("Payment amount is less than outstanding balance.");

        ex.StatusCode.Should().Be(422);
        ex.Code.Should().Be("business_rule_violation");
        ex.Message.Should().Contain("outstanding balance");
    }

    [Fact]
    public void ConflictException_HasCorrectStatusAndCode()
    {
        var ex = new ConflictException("Seat already booked");

        ex.StatusCode.Should().Be(409);
        ex.Code.Should().Be("conflict");
    }

    [Fact]
    public void ForbiddenException_HasCorrectStatusAndCode()
    {
        var ex = new ForbiddenException("Access denied");

        ex.StatusCode.Should().Be(403);
        ex.Code.Should().Be("forbidden");
    }

    [Fact]
    public void ValidationException_DictConstructor_SetsAllProperties()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required.", "Email is invalid."],
            ["Password"] = ["Password too short."]
        };

        var ex = new ValidationException("Validation failed.", errors);

        ex.StatusCode.Should().Be(400);
        ex.Code.Should().Be("validation_error");
        ex.Message.Should().Be("Validation failed.");
        ex.Errors["Email"].Should().BeEquivalentTo(["Email is required.", "Email is invalid."]);
        ex.Errors["Password"].Should().BeEquivalentTo(["Password too short."]);
    }

    [Fact]
    public void ValidationException_FieldErrorConstructor_BuildsSingleEntryDict()
    {
        var ex = new ValidationException("FullName", "FullName is required.");

        ex.StatusCode.Should().Be(400);
        ex.Code.Should().Be("validation_error");
        ex.Errors.Should().HaveCount(1);
        ex.Errors["FullName"].Should().BeEquivalentTo(["FullName is required."]);
    }

    [Fact]
    public void AllExceptions_InheritFromAppException()
    {
        new NotFoundException("").Should().BeAssignableTo<AppException>();
        new BusinessRuleException("").Should().BeAssignableTo<AppException>();
        new ConflictException("").Should().BeAssignableTo<AppException>();
        new ForbiddenException("").Should().BeAssignableTo<AppException>();
        new ValidationException("f", "e").Should().BeAssignableTo<AppException>();
    }

    [Fact]
    public void AllExceptions_InheritFromSystemException()
    {
        new NotFoundException("").Should().BeAssignableTo<Exception>();
        new BusinessRuleException("").Should().BeAssignableTo<Exception>();
    }
}
