
using Sitecore.Commerce.Connect.CommerceServer.Search.Models;
using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.SecurityModel;

namespace Sitecore.Support.Commerce.Connect.CommerceServer.Search
{
    public class SelectedCatalogsCrawler : Sitecore.Commerce.Connect.CommerceServer.Search.SelectedCatalogsCrawler
    {
        protected override CommerceIndexableItem GetIndexable(IIndexableUniqueId indexableUniqueId)
        {
            using (new SecurityDisabler())
            {
                ItemUri itemUri = indexableUniqueId as SitecoreItemUniqueId;
                if (itemUri != null)
                {
                    return new CommerceIndexableItem(Sitecore.Data.Database.GetItem(itemUri));
                }
                else
                {
                    return null;
                }
            }
        }
    }
}