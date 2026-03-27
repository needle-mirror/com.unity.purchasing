using System;
using UnityEngine.Purchasing.GoogleBilling.Models;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.GoogleBilling.Interfaces
{
    internal interface IExternalBillingProgramClientInternal : IBillingClientBase
    {
        void IsBillingProgramAvailableAsync(Action<IGoogleBillingResult> onBillingProgramAvailabilityResponse);

        void LaunchExternalLink(
            string externalLinkUrl,
            Action<IGoogleBillingResult> onLaunchExternalLinkResponse,
            LinkType linkType,
            LaunchMode launchMode
        );

       void CreateBillingProgramReportingDetailsAsync(
            Action<IGoogleBillingResult, string> onCreateBillingProgramReportingDetailsResponse);
    }
}
