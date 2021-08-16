using ErtisAuth.Core.Models.Roles;
using NUnit.Framework;

namespace ErtisAuth.Tests.Shared.Core.AccessControl
{
	[TestFixture]
	public class RbacTests
	{
		#region Methods

		[Test]
		public void RbacEquality_WithOperator_AllSegmentsSame_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.*");
			var rbac2 = Rbac.Parse("*.*.*.*");
			Assert.True(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithOperator_AllSegmentsSame_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.users.read.*");
			var rbac2 = Rbac.Parse("*.users.read.*");
			Assert.True(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_AllSegmentsSame_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.*");
			var rbac2 = Rbac.Parse("*.*.*.*");
			Assert.True(rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithEquals_AllSegmentsSame_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.users.read.*");
			var rbac2 = Rbac.Parse("*.users.read.*");
			Assert.True(rbac1.Equals(rbac2));
		}

		[Test]
		public void RbacEquality_WithOperator_SubjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("987.*.*.*");
			var rbac2 = Rbac.Parse("123.*.*.*");
			Assert.False(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_SubjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("987.*.*.*");
			var rbac2 = Rbac.Parse("123.*.*.*");
			Assert.False(rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithOperator_ResourceSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.123.*.*");
			var rbac2 = Rbac.Parse("*.987.*.*");
			Assert.False(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_ResourceSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.123.*.*");
			var rbac2 = Rbac.Parse("*.987.*.*");
			Assert.False(rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithOperator_ActionSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.123.*");
			var rbac2 = Rbac.Parse("*.*.987.*");
			Assert.False(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_ActionSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.123.*");
			var rbac2 = Rbac.Parse("*.*.987.*");
			Assert.False(rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithOperator_ObjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.False(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_ObjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.False(rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_NullCheck_ReturnTrue_Test()
		{
			Rbac rbac = null;
			Assert.True(rbac == null);
		}
		
		[Test]
		public void RbacEquality_NullCheck_ReturnFalse_Test()
		{
			Rbac rbac = null;
			Assert.False(rbac != null);
		}
		
		[Test]
		public void RbacEquality_NullCheck_WithNotNull_ReturnTrue_Test()
		{
			var rbac = Rbac.Parse("*.*.*.123");
			Assert.True(rbac != null);
		}
		
		[Test]
		public void RbacEquality_NullCheck_WithNotNull_ReturnFalse_Test()
		{
			var rbac = Rbac.Parse("*.*.*.123");
			Assert.False(rbac == null);
		}
		
		[Test]
		public void RbacSegmentEquality_AllSegmentCheck_ReturnTrue_Test()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			Assert.True(rbac1.Resource == RbacSegment.All);
		}
		
		[Test]
		public void RbacSegmentEquality_WithOperator_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.True(rbac1.Action == rbac2.Action);
		}
		
		[Test]
		public void RbacSegmentEquality_WithEquals_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.True(rbac1.Action.Equals(rbac2.Action));
		}
		
		[Test]
		public void RbacSegmentEquality_WithOperator_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.123");
			Assert.True(rbac1.Object == rbac2.Object);
		}
		
		[Test]
		public void RbacSegmentEquality_WithEquals_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.123");
			Assert.True(rbac1.Object.Equals(rbac2.Object));
		}
		
		[Test]
		public void RbacSegmentEquality_WithOperator_ReturnFalse_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.False(rbac1.Object == rbac2.Object);
		}
		
		[Test]
		public void RbacSegmentEquality_WithEquals_ReturnFalse_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.False(rbac1.Object.Equals(rbac2.Object));
		}
		
		[Test]
		public void Rbac_ToString_Test()
		{
			var rbac = Rbac.Parse("123.users.create.987");
			Assert.AreEqual("123.users.create.987", rbac.ToString());
		}
		
		#endregion
	}
}