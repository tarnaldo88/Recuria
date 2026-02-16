using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;
using Recuria.Blazor.Pages;
using Recuria.Blazor.Services.App;

namespace Recuria.Tests.Unit.Frontend
{
    public sealed class OpsServerTableBehaviorTests : TestContext
    {
        public OpsServerTableBehaviorTests()
        {
            Services.AddMudServices();
        }
    }
}
