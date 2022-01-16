using ErtisAuth.Core.Models.Roles;

namespace ErtisAuth.Hub.ViewModels
{
    public class AuthorizedButtonViewModel
    {
        #region Properties

        public string ResourceId { get; private set; }
        
        public string CollectionName { get; private set; }
        
        public string ResourceName { get; private set; }
        
        public string ResourceTitle { get; private set; }

        public RbacSegment RbacAction { get; private set; }

        public string FormId { get; private set; }
        
        public string ModalId { get; private set; }

        public string Text { get; private set; }

        public string CssClass { get; init; } = "success";
        
        public string BootstrapIcon { get; init; }

        public bool IsSmall { get; init; } = true;
        
        public bool IsWidest { get; init; } = true;
        
        public bool HideIfForbidden { get; init; }
        
        #endregion

        #region Constructors

        private AuthorizedButtonViewModel()
        { }

        #endregion
        
        #region Methods

        public static AuthorizedButtonViewModel CreateButton(string collectionName, string text, string modalId)
        {
            return new AuthorizedButtonViewModel
            {
                CollectionName = collectionName,
                RbacAction = Rbac.CrudActionSegments.Create,
                ModalId = modalId,
                Text = text,
                CssClass = "primary",
                BootstrapIcon = "plus-lg",
                IsSmall = true,
                IsWidest = true
            };
        }
        
        public static AuthorizedButtonViewModel SaveButton(string resourceId, string collectionName, string formId)
        {
            return new AuthorizedButtonViewModel
            {
                ResourceId = resourceId,
                CollectionName = collectionName,
                RbacAction = Rbac.CrudActionSegments.Update,
                FormId = formId,
                Text = "Save",
                CssClass = "success",
                BootstrapIcon = "hdd",
                IsSmall = true,
                IsWidest = true
            };
        }
        
        public static AuthorizedButtonViewModel DeleteModalButton(string resourceId, string collectionName, string resourceName, string resourceTitle, bool hideIfForbidden = false)
        {
            return new AuthorizedButtonViewModel
            {
                ResourceId = resourceId,
                CollectionName = collectionName,
                RbacAction = Rbac.CrudActionSegments.Delete,
                ResourceName = resourceName,
                ResourceTitle = resourceTitle,
                HideIfForbidden = hideIfForbidden
            };
        }

        #endregion
    }
}