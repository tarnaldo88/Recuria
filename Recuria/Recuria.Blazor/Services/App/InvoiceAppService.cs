namespace Recuria.Blazor.Services.App
{
    public interface IInvoiceAppService
    {
        Task<AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>> GetByOrganizationAsync(Guid organizationId, bool notifyError = true);
        Task<AppResult<Guid>> CreateAsync(Recuria.Client.CreateInvoiceRequest request, bool notifySuccess = true);
        Task<AppResult<Recuria.Client.InvoiceDetailsDto>> GetByIdAsync(Guid invoiceId, bool notifyError = true);

        Task<AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>> GetPageAsync(
            Guid organizationId,
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true);
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

        public async Task<AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>> GetPageAsync(
            Guid organizationId,
            int page,
            int pageSize,
            string? search,
            string? sortBy,
            string? sortDir,
            bool notifyError = true)
        {
            var result = await _runner.RunAsync(
                () => _api.OrganizationAsync(organizationId),
                errorPrefix: "Unable to load invoices",
                notifyError: notifyError);

            if (!result.Success || result.Data is null)
                return AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>.Fail(result.Error ?? "Unable to load invoices");

            IEnumerable<Recuria.Client.InvoiceListItemDto> query = result.Data;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(x =>
                    (x.Status ?? string.Empty).Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            query = (sortBy?.ToLowerInvariant(), sortDir?.ToLowerInvariant()) switch
            {
                ("total", "desc") => query.OrderByDescending(x => x.Total?.Amount ?? 0),
                ("total", _) => query.OrderBy(x => x.Total?.Amount ?? 0),
                ("status", "desc") => query.OrderByDescending(x => x.Status),
                ("status", _) => query.OrderBy(x => x.Status),
                ("issuedonutc", "desc") => query.OrderByDescending(x => x.IssuedOnUtc),
                ("issuedonutc", _) => query.OrderBy(x => x.IssuedOnUtc),
                _ => query.OrderByDescending(x => x.IssuedOnUtc)
            };

            var safePage = Math.Max(1, page);
            var safePageSize = Math.Max(1, pageSize);
            var paged = query.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList();

            return AppResult<ICollection<Recuria.Client.InvoiceListItemDto>>.Ok(paged);
        }

    }
}
