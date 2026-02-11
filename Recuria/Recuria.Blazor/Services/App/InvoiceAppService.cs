namespace Recuria.Blazor.Services.App
{
    public interface IInvoiceAppService
    {
        Task<AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>> GetByOrganizationAsync(Guid organizationId, bool notifyError = true);
        Task<AppResult<Guid>> CreateAsync(Recuria.Client.CreateInvoiceRequest request, bool notifySuccess = true);
        Task<AppResult<Recuria.Client.InvoiceDetailsDto>> GetByIdAsync(Guid invoiceId, bool notifyError = true);
    }

    public sealed class InvoiceAppService : IInvoiceAppService
    {
        private readonly Recuria.Client.IRecuriaApiClient _api;
        private readonly ApiCallRunner _runner;

        public InvoiceAppService(Recuria.Client.IRecuriaApiClient api, ApiCallRunner runner)
        {
            _api = api;
            _runner = runner;
        }

        public Task<AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>> GetByOrganizationAsync(Guid organizationId, bool notifyError = true) =>
            _runner.RunAsync(() => _api.OrganizationAsync(organizationId), errorPrefix: "Unable to load invoices", notifyError: notifyError);

        public Task<AppResult<Guid>> CreateAsync(Recuria.Client.CreateInvoiceRequest request, bool notifySuccess = true) =>
            _runner.RunAsync(
                () => _api.InvoicesPOSTAsync(request),
                successMessage: "Invoice created.",
                errorPrefix: "Unable to create invoice",
                notifySuccess: notifySuccess,
                notifyError: true);

        public Task<AppResult<Recuria.Client.InvoiceDetailsDto>> GetByIdAsync(Guid invoiceId, bool notifyError = true) =>
            _runner.RunAsync(() => _api.InvoicesGETAsync(invoiceId), errorPrefix: "Unable to load invoice", notifyError: notifyError);
    }
}
