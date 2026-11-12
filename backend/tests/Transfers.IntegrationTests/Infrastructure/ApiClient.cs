using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Application.Transfers;

namespace Transfers.IntegrationTests.Infrastructure;

public sealed record RejectedTransfer(int Status, string Type, string Detail, TransferResponse Transfer);

public sealed class ApiClient(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string[] Documents = CpfGenerator.Take(500).ToArray();
    private static int _documentIndex;

    public HttpClient Http => http;

    public async Task<AccountResponse> OpenAccountAsync(
        decimal balance = 0m,
        decimal overdraftLimit = 0m,
        object? daytimeLimit = null,
        object? nighttimeLimit = null,
        string name = "Titular Teste")
    {
        var document = Documents[Interlocked.Increment(ref _documentIndex)];
        var response = await http.PostAsJsonAsync("/api/accounts", new
        {
            holderName = name,
            holderDocument = document,
            initialBalance = balance,
            overdraftLimit,
            daytimeLimit,
            nighttimeLimit
        }, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AccountResponse>(Json))!;
    }

    public async Task<AccountDetailsResponse> GetAccountAsync(Guid id) =>
        (await http.GetFromJsonAsync<AccountDetailsResponse>($"/api/accounts/{id}", Json))!;

    public Task<HttpResponseMessage> TransferAsync(Guid source, Guid destination, decimal amount, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Content = JsonContent.Create(new { sourceAccountId = source, destinationAccountId = destination, amount }, options: Json)
        };

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return http.SendAsync(request);
    }

    public Task<HttpResponseMessage> ScheduleAsync(Guid source, Guid destination, decimal amount, DateTimeOffset scheduledFor) =>
        http.PostAsJsonAsync("/api/transfers/scheduled", new { sourceAccountId = source, destinationAccountId = destination, amount, scheduledFor }, Json);

    public async Task<TransferResponse> GetTransferAsync(Guid id) =>
        (await http.GetFromJsonAsync<TransferResponse>($"/api/transfers/{id}", Json))!;

    public async Task<PagedResponse<T>> GetPageAsync<T>(string url) =>
        (await http.GetFromJsonAsync<PagedResponse<T>>(url, Json))!;

    public static async Task<TransferResponse> ReadTransferAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<TransferResponse>(Json))!;

    public static async Task<RejectedTransfer> ReadRejectionAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RejectedTransfer>(Json))!;
    }
}
