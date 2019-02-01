namespace Clipbox.ViewModels.BindableObjects
{
    public class ClipboardViewSubItemNMore : ClipboardViewSubItemBase
    {
        private int _numberOfItems;
        /// <summary>
        /// The number of items to show in "+N items"
        /// </summary>
        public int NumberOfItems
        {
            get { return _numberOfItems; }
            set { SetField(ref _numberOfItems, value); }
        }
    }
}
