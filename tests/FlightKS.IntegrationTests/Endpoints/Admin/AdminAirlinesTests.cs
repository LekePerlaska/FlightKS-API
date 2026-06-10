using System.Net;
using System.Net.Http.Json;
using FlightKS.IntegrationTests.Infrastructure;

namespace FlightKS.IntegrationTests.Endpoints.Admin;

public class AdminAirlinesTests(IntegrationWebAppFactory factory) : IntegrationTestBase(factory)
{
    private HttpClient AdminClient => CreateClientWithRoles("Admin");

    [Fact]
    public async Task GetAll_Returns200()
    {
        var response = await AdminClient.GetAsync("/api/v1/admin/airlines");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        var payload = new { Code = "EK", Name = "Emirates", Country = "UAE" };
        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airlines", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"EK\"");
    }

    [Fact]
    public async Task Create_InvalidBody_Returns400()
    {
        var payload = new { Code = "", Name = "", Country = "UAE" };
        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airlines", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("validation_error");
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns409()
    {
        await using var db = CreateDb();
        await new IntegrationSeeder(db).AirlineAsync("QR", "Qatar Airways");

        var payload = new { Code = "QR", Name = "Other Airline", Country = "Country" };
        var response = await AdminClient.PostAsJsonAsync("/api/v1/admin/airlines", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_Then_Restore_Works()
    {
        await using var db = CreateDb();
        var airline = await new IntegrationSeeder(db).AirlineAsync("RJ", "Royal Jordanian");

        var deleteResp = await AdminClient.DeleteAsync($"/api/v1/admin/airlines/{airline.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK); // airlines return 200 with message on delete

        var restoreResp = await AdminClient.PatchAsync($"/api/v1/admin/airlines/{airline.Id}/restore", null);
        restoreResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
