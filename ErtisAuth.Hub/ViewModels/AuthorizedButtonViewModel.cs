using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Hub.Extensions;

namespace ErtisAuth.Hub.ViewModels
{
    public static class AuthorizedButton
    {
        #region Methods

        public static AuthorizedCustomButtonViewModel CustomButton(
            string text, 
            AuthorizedButtonStyleOptions styleOptions,
            RbacSegment subjectSegment, 
            RbacSegment resourceSegment, 
            RbacSegment actionSegment, 
            RbacSegment objectSegment)
        {
            return new AuthorizedCustomButtonViewModel(
                text, 
                styleOptions,
                subjectSegment, 
                resourceSegment, 
                actionSegment, 
                objectSegment);
        }

        public static AuthorizedModalButtonViewModel CreateButton(string modalId, string rbacResource)
        {
            return new AuthorizedModalButtonViewModel(
                modalId,
                RbacSegment.All,
                new RbacSegment(rbacResource),
                Rbac.CrudActionSegments.Create,
                RbacSegment.All)
            {
                Text = $"Create {rbacResource.TrimEnd('s').ToLower().Capitalize()}",
                StyleOptions = new AuthorizedButtonStyleOptions
                {
                    CssClass = "primary",
                    BootstrapIcon = "plus-lg",
                    IsSmall = true,
                    IsWidest = true
                }
            };
        }
        
        public static AuthorizedFormButtonViewModel SaveButton(string formId, string rbacResource, string rbacObject)
        {
            return new AuthorizedFormButtonViewModel(
                formId, 
                RbacSegment.All, 
                new RbacSegment(rbacResource),
                Rbac.CrudActionSegments.Update, 
                new RbacSegment(rbacObject))
            {
                Text = "Save",
                StyleOptions = new AuthorizedButtonStyleOptions
                {
                    CssClass = "success",
                    BootstrapIcon = "hdd",
                    IsSmall = true,
                    IsWidest = true
                }
            };
        }
        
        public static AuthorizedDeleteModalButtonViewModel DeleteModalButton(string itemId, string itemName, string itemTypeName, string rbacResource, string rbacObject)
        {
            return new AuthorizedDeleteModalButtonViewModel(
                itemId,
                itemName,
                itemTypeName,
                new RbacSegment(rbacResource),
                new RbacSegment(rbacObject))
            {
                HideIfForbidden = true
            };
        }

        #endregion
    }

    public interface IAuthorizedButtonViewModel
    {
        #region Properties
        
        string Text { get; }
        
        Rbac Rbac { get; }
        
        bool HideIfForbidden { get; }
        
        #endregion
    }
    
    public interface IStylizedAuthorizedButton
    {
        #region Properties
        
        AuthorizedButtonStyleOptions StyleOptions { get; }
        
        #endregion
    }
    
    public abstract class AuthorizedButtonViewModelBase : IAuthorizedButtonViewModel, IStylizedAuthorizedButton
    {
        #region Properties
        
        public string Text { get; init; }

        public AuthorizedButtonStyleOptions StyleOptions { get; init; }
        
        public bool HideIfForbidden { get; init; }
        
        public Rbac Rbac { get; }
        
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="subjectSegment"></param>
        /// <param name="resourceSegment"></param>
        /// <param name="actionSegment"></param>
        /// <param name="objectSegment"></param>
        protected AuthorizedButtonViewModelBase(RbacSegment subjectSegment, RbacSegment resourceSegment, RbacSegment actionSegment, RbacSegment objectSegment)
        {
            this.Rbac = new Rbac(subjectSegment, resourceSegment, actionSegment, objectSegment);
        }

        #endregion
    }
    
    public class AuthorizedCustomButtonViewModel : AuthorizedButtonViewModelBase
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="styleOptions"></param>
        /// <param name="subjectSegment"></param>
        /// <param name="resourceSegment"></param>
        /// <param name="actionSegment"></param>
        /// <param name="objectSegment"></param>
        public AuthorizedCustomButtonViewModel(
            string text, 
            AuthorizedButtonStyleOptions styleOptions,
            RbacSegment subjectSegment, 
            RbacSegment resourceSegment, 
            RbacSegment actionSegment, 
            RbacSegment objectSegment) : 
            base(subjectSegment, resourceSegment, actionSegment, objectSegment)
        {
            this.Text = text;
            this.StyleOptions = styleOptions;
        }

        #endregion
    }
    
    public class AuthorizedFormButtonViewModel : AuthorizedButtonViewModelBase
    {
        #region Properties
        
        public string FormId { get; }
        
        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="formId"></param>
        /// <param name="subjectSegment"></param>
        /// <param name="resourceSegment"></param>
        /// <param name="actionSegment"></param>
        /// <param name="objectSegment"></param>
        public AuthorizedFormButtonViewModel(string formId, RbacSegment subjectSegment, RbacSegment resourceSegment, RbacSegment actionSegment, RbacSegment objectSegment) : 
            base(subjectSegment, resourceSegment, actionSegment, objectSegment)
        {
            this.FormId = formId;
        }

        #endregion
    }
    
    public class AuthorizedModalButtonViewModel : AuthorizedButtonViewModelBase
    {
        #region Properties
        
        public string ModalId { get; }
        
        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modalId"></param>
        /// <param name="subjectSegment"></param>
        /// <param name="resourceSegment"></param>
        /// <param name="actionSegment"></param>
        /// <param name="objectSegment"></param>
        public AuthorizedModalButtonViewModel(string modalId, RbacSegment subjectSegment, RbacSegment resourceSegment, RbacSegment actionSegment, RbacSegment objectSegment) : 
            base(subjectSegment, resourceSegment, actionSegment, objectSegment)
        {
            this.ModalId = modalId;
        }

        #endregion
    }
    
    public class AuthorizedDeleteModalButtonViewModel : AuthorizedModalButtonViewModel
    {
        #region Properties
        
        public string ItemId { get; }
        
        public string ItemName { get; }
        
        public string ItemTypeName { get; }
        
        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="itemName"></param>
        /// <param name="itemTypeName"></param>
        /// <param name="rbacResource"></param>
        /// <param name="rbacObject"></param>
        public AuthorizedDeleteModalButtonViewModel(string itemId, string itemName, string itemTypeName, string rbacResource, string rbacObject) :
            base("itemDeleteModal", RbacSegment.All, new RbacSegment(rbacResource), Rbac.CrudActionSegments.Delete, new RbacSegment(rbacObject))
        {
            this.ItemId = itemId;
            this.ItemName = itemName;
            this.ItemTypeName = itemTypeName;
        }

        #endregion
    }
    
    public class AuthorizedButtonStyleOptions
    {
        #region Properties
        
        public string CssClass { get; init; } = "success";
        
        public string BootstrapIcon { get; init; }

        public bool IsSmall { get; init; } = true;
        
        public bool IsWidest { get; init; } = true;
        
        #endregion
    }
}