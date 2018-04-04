
using Sitecore.Commerce.Connect.CommerceServer.Search.Models;
using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.Data.Items;
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
                    Item item = Sitecore.Data.Database.GetItem(itemUri);
                    if (item != null)
                    {
                        return new CommerceIndexableItem(item);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}