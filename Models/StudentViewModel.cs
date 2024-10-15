using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ELearning.Models
{
    public class StudentViewModel
    {
        public IPagedList<StudentModel> Students { get; set; }
        public IEnumerable<SelectListItem> ClassList { get; set; }
    }

    public class StudentModel
    {
        public int ID { get; set; } = 0;
        public string UserName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int ClassID { get; set; } = 0;
        public string ClassName { get; set; } = "";
        public bool IsVerified { get; set; } = false;
        public string DefaultImageName { get; set; } = "";
        public HttpPostedFileBase ImageFile { get; set; }
        public int UpdatedBy { get; set; } = 0;
        public string Password { get; set; } = "";
        public bool IsActive { get; set; } = false;

        public IEnumerable<SelectListItem> ClassList { get; set; }

    }

    public class StudentTrackingModel
    {
        public string StoryTitle { get; set; } = "";
        public string StudentName { get; set; } = "";
        public decimal ResultPercentage { get; set; } = 0;
    }


}