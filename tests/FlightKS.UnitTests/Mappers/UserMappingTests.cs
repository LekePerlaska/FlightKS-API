using FlightKS.Mappers;
using FlightKS.Models.Entities;

namespace FlightKS.UnitTests.Mappers;

public class UserMappingTests
{
    [Fact]
    public void ToResponse_MapsScalarFields()
    {
        var user = E.User("jane@example.com", "Jane Doe");
        user.PhoneNumber = "+1234567890";
        user.DateOfBirth = new DateOnly(1990, 5, 15);
        user.PassportNumber = "AB123456";
        user.Nationality = "British";

        var dto = user.ToResponse(["User", "Admin"]);

        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be("jane@example.com");
        dto.FullName.Should().Be("Jane Doe");
        dto.PhoneNumber.Should().Be("+1234567890");
        dto.DateOfBirth.Should().Be(new DateOnly(1990, 5, 15));
        dto.PassportNumber.Should().Be("AB123456");
        dto.Nationality.Should().Be("British");
        dto.IsActive.Should().Be(user.IsActive);
        dto.CreatedAt.Should().Be(user.CreatedAt);
        dto.UpdatedAt.Should().Be(user.UpdatedAt);
        dto.Roles.Should().BeEquivalentTo(["User", "Admin"]);
    }

    [Fact]
    public void ToResponse_NullRoles_ReturnsEmptyList()
    {
        var user = E.User();

        var dto = user.ToResponse(null);

        dto.Roles.Should().BeEmpty();
    }

    [Fact]
    public void ToResponse_OnlyPassportDocsIncluded()
    {
        var user = E.User();
        var passportDoc = E.PassportDoc(user.Id);
        var otherDoc = new UploadedFile
        {
            Id = Guid.NewGuid(),
            UploadedByUserId = user.Id,
            FileName = "other.pdf",
            OriginalFileName = "other.pdf",
            ContentType = "application/pdf",
            SizeBytes = 100,
            StoragePath = "/uploads/other.pdf",
            RelatedEntityName = "SomeOtherType"
        };
        user.UploadedFiles = [passportDoc, otherDoc];

        var dto = user.ToResponse();

        dto.Documents.Should().HaveCount(1);
        dto.Documents[0].Id.Should().Be(passportDoc.Id);
    }

    [Fact]
    public void ToResponse_DocumentsOrderedByCreatedAtDescending()
    {
        var user = E.User();
        var older = E.PassportDoc(user.Id, createdAt: DateTime.UtcNow.AddDays(-2));
        var newer = E.PassportDoc(user.Id, createdAt: DateTime.UtcNow.AddDays(-1));
        user.UploadedFiles = [older, newer];

        var dto = user.ToResponse();

        dto.Documents[0].Id.Should().Be(newer.Id);
        dto.Documents[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public void ToResponse_DocumentDto_MapsAllFields()
    {
        var user = E.User();
        var doc = E.PassportDoc(user.Id);
        doc.RelatedEntityId = Guid.NewGuid();
        user.UploadedFiles = [doc];

        var docDto = user.ToResponse().Documents[0];

        docDto.Id.Should().Be(doc.Id);
        docDto.FileName.Should().Be(doc.FileName);
        docDto.OriginalFileName.Should().Be(doc.OriginalFileName);
        docDto.ContentType.Should().Be(doc.ContentType);
        docDto.SizeBytes.Should().Be(doc.SizeBytes);
        docDto.RelatedEntityName.Should().Be("UserPassportDocument");
        docDto.RelatedEntityId.Should().Be(doc.RelatedEntityId);
        docDto.CreatedAt.Should().Be(doc.CreatedAt);
    }

    [Fact]
    public void ToResponse_NoFiles_EmptyDocuments()
    {
        var user = E.User();

        user.ToResponse().Documents.Should().BeEmpty();
    }
}
