namespace Recuria.Blazor.Services.App
{
    public interface IInvoiceAppService
    {
        Task<AppResult<Guid>> CreateAsync(Recuria.Client.CreateInvoiceRequest request, bool notifySuccess = true);
        Task<AppResult<Recuria.Client.InvoiceDetailsDto>> GetByIdAsync(Guid invoiceId, bool notifyError = true);

        Task<AppResult<Recuria.Client.InvoiceListItemDtoPagedResult>> GetPageAsync(
            Guid organizationId,
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true);

        Task<AppResult> MarkAsPaidAsync(Guid invoiceId);
        Task<AppResult> VoidAsync(Guid invoiceId);
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

        public Task<AppResult<Recuria.Client.InvoiceDetailsDto>> GetByIdAsync(Guid invoiceId, bool notifyError = true) =>
            _runner.RunAsync(() => _api.InvoicesGETAsync(invoiceId), errorPrefix: "Unable to load invoice", notifyError: notifyError);

        public Task<AppResult<Recuria.Client.InvoiceListItemDtoPagedResult>> GetPageAsync(
            Guid organizationId,
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true) =>
            _runner.RunAsync(
                () => _api.OrganizationAsync(organizationId, page, pageSize, search, sortBy, sortDir),
                errorPrefix: "Unable to load invoices",
                notifyError: notifyError);

        public Task<AppResult<Guid>> CreateAsync(Recuria.Client.CreateInvoiceRequest request, bool notifySuccess = true) =>
            _runner.RunAsync(
                () => _api.InvoicesPOSTAsync(Guid.NewGuid().ToString("N"), request),
                successMessage: "Invoice created.",
                errorPrefix: "Unable to create invoice",
                notifySuccess: notifySuccess,
                notifyError: true);

        public Task<AppResult> MarkAsPaidAsync(Guid invoiceId) =>
            _runner.RunAsync(
                () => _api.PayAsync(invoiceId),
                successMessage: "Invoice marked as paid.",
                errorPrefix: "Unable to mark invoice as paid",
                notifySuccess: true,
                notifyError: true);

        public Task<AppResult> VoidAsync(Guid invoiceId) =>
            _runner.RunAsync(
                () => _api.VoidAsync(invoiceId),
                successMessage: "Invoice voided.",
                errorPrefix: "Unable to void invoice",
                notifySuccess: true,
                notifyError: true);
    }
}
