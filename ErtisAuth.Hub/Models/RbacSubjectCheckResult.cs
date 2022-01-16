using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Hub.Models
{
    public class RbacSubjectCheckResult
    {
        #region Properties

        public string UtilizerId { get; set; }
		
        public string RoleId { get; set; }
		
        public Utilizer.UtilizerType UtilizerType { get; set; }
		
        public string UtilizerName { get; set; }
		
        public string Message { get; set; }

        public RbacSubjectRejectReason Reason { get; set; }

        #endregion

        #region Enums

        public enum RbacSubjectRejectReason
        {
            UtilizerNotFound,
            RoleNotFound,
            OutOfRole,
        }

        #endregion
    }
}