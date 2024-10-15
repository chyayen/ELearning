using ELearning.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ELearning.Controllers
{
    public class AccountController : Controller
    {
        private string usertype = "student";
        private string defaultConnection = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        // GET: Account
        public ActionResult Index()
        {
            if (Session["UserName"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        [HttpPost] 
        public ActionResult AccountUpdateProfile(string fullName, string email, HttpPostedFileBase defaultImage)
        {
            if (Session["UserName"] != null)
            {
                int userid = (int)Session["UserID"];
                var username = Session["UserName"];
                var defaultImageName = "";
                if (defaultImage != null && defaultImage.ContentLength > 0)
                {
                    string filetype = Path.GetExtension(defaultImage.FileName);
                    if (filetype.Contains(".jpg") || filetype.Contains(".jpeg") || filetype.Contains(".png"))
                    {
                        defaultImageName = username + filetype;
                    }
                } 
                 
                if(ProfileUpdate(userid, fullName, email, defaultImageName) > 0)
                {
                    if (defaultImage != null && defaultImage.ContentLength > 0)
                    {
                        string path = Server.MapPath("~/Images/Users/" + defaultImageName);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);

                        defaultImage.SaveAs(path);
                        Session["DefaultImageName"] = defaultImageName;
                    }

                    Session["FullName"] = fullName;
                    Session["Email"] = email; 
                }

                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        [HttpPost] 
        public ActionResult AccountUpdatePassword(FormCollection form)
        {
            if (Session["UserName"] != null)
            {
                var currentPassword = form["CurrentPassword"];
                var newPassword = form["NewPassword"];
                var confirmNewPassword = form["ConfirmNewPassword"];
                int userid = (int)Session["UserID"];

                if (newPassword == confirmNewPassword)
                {
                    if(PasswordUpdate(userid, currentPassword, newPassword) > 0)
                    {
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    ViewData["AlertMessage"] = "<p class='alert alert-danger'>New passwords do not match.</p>";
                }

                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public ActionResult Login()
        {
            if (Session["UserName"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            var userdb = GetUserLogin(model.UserName, model.Password);

            if (userdb != null && !string.IsNullOrEmpty(userdb.UserName))
            {
                if (userdb.IsVerified)
                {
                    Session.Timeout = 3600;
                    Session["UserID"] = userdb.UserID;
                    Session["UserName"] = userdb.UserName;
                    Session["FullName"] = userdb.FullName;
                    Session["Email"] = userdb.Email;
                    Session["UserType"] = userdb.UserType;
                    Session["ClassID"] = userdb.ClassID;
                    Session["DefaultImageName"] = userdb.DefaultImageName;
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewData["AlertMessage"] = "<p class='alert alert-danger'>Your account needs verification. Please contact the school administrator or wait 2-3 days for your account to be verified.</p>";
                }
            }
            else
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>Incorrect username or password.</p>";
            }

            return View(model);
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult Logout(LoginViewModel model)
        {
            Session.Abandon(); // it will clear the session at the end of request
            return RedirectToAction("Login");
        }

        public ActionResult Register()
        {
            RegisterViewModel model = new RegisterViewModel();
            model.UserType = usertype;
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (UserInsertDB(model) > 0)
            {
                ViewData["AlertMessage"] = "<p class='alert alert-success'>Your account was successfully created. Please contact the school administrator or wait 2-3 days for your account to be verified.</p>";
                return RedirectToAction("Login");
            }

            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }

        #region Helpers
        private LoginViewModel GetUserLogin(string username, string password)
        {
            LoginViewModel model = new LoginViewModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select u.*, sc.classid from users u left join studentclasses sc on sc.studentid = u.id where u.username = @user and u.password = @pass and u.usertype = @logintype";
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", password);
            cmd.Parameters.AddWithValue("@logintype", usertype);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    model.UserID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.UserName = rd["username"] != null ? rd["username"].ToString() : "";
                    model.Password = rd["password"] != null ? rd["password"].ToString() : "";
                    model.IsVerified = rd["isverified"] != null ? (bool)rd["isverified"] : false;
                    model.FullName = rd["name"] != null ? rd["name"].ToString() : "";
                    model.Email = rd["email"] != null ? rd["email"].ToString() : "";
                    model.UserType = rd["usertype"] != null ? rd["usertype"].ToString() : "";
                    model.ClassID = rd["classid"] != null && rd["classid"].ToString() != "" ? Convert.ToInt32(rd["classid"].ToString()) : 0;
                    model.DefaultImageName = rd["defaultimagename"] != null ? rd["defaultimagename"].ToString() : "";
                    model.IsActive = rd["isactive"] != null ? (bool)rd["isactive"] : false;
                    rd.Close();
                }
            }
            catch (Exception ex)
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + ex.Message + "</p>";
            }
            finally
            {
                con.Close();
            }
            return model;
        }

        private int UserInsertDB(RegisterViewModel model)
        {
            int count = 0;
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"INSERT INTO users(username, name, email, password, isverified, usertype, defaultimagename, isactive) 
                                VALUES(@username, @name, @email, @password, @isverified, @usertype, '', 1);

                                INSERT INTO studentclasses(studentid, classid)
                                VALUES(LAST_INSERT_ID(), @classid);";
            cmd.Parameters.AddWithValue("@username", model.UserName);
            cmd.Parameters.AddWithValue("@name", model.FullName);
            cmd.Parameters.AddWithValue("@email", model.Email);
            cmd.Parameters.AddWithValue("@password", model.Password);
            cmd.Parameters.AddWithValue("@usertype", usertype);
            cmd.Parameters.AddWithValue("@isverified", false);
            cmd.Parameters.AddWithValue("@classid", model.ClassID);

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                if (count > 0)
                {
                    ViewData["AlertMessage"] = "<p class='alert alert-success'>Your registration is successful.</p>";
                } 
            }
            catch (Exception ex)
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + ex.Message + "</p>";
            }
            finally
            {
                con.Close();
            }
            return count;

        }

        private List<ClassModel> GetAllClasses()
        {
            List<ClassModel> list = new List<ClassModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select * from classes";

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    ClassModel model = new ClassModel();
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.Name = rd["name"] != null ? rd["name"].ToString() : "";
                    list.Add(model);
                }
                rd.Close();
            }
            finally
            {
                con.Close();
            }
            return list;
        }

        private int ProfileUpdate(int userid, string fullName, string email, string defaultImageName)
        {
            int count = 0;
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"UPDATE users SET name = @name, email = @email, defaultimagename = (case when @defaultimagename is null or @defaultimagename = '' then defaultimagename else @defaultimagename end)
                                WHERE id = @userid";
            cmd.Parameters.AddWithValue("@userid", userid);
            cmd.Parameters.AddWithValue("@name", fullName);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@defaultimagename", defaultImageName); 

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                if (count > 0)
                {
                    ViewData["AlertMessage"] = "<p class='alert alert-success'>Your profile update is successful.</p>";
                }
            }
            catch (Exception ex)
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + ex.Message + "</p>";
            }
            finally
            {
                con.Close();
            }
            return count;

        }
        private int PasswordUpdate(int userid, string currentPassword, string newPassword)
        {
            int count = 0;
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"UPDATE users SET password = @newPassword WHERE id = @userid and password = @currentPassword";
            cmd.Parameters.AddWithValue("@userid", userid);
            cmd.Parameters.AddWithValue("@newPassword", newPassword);
            cmd.Parameters.AddWithValue("@currentPassword", currentPassword);

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                if (count > 0)
                {
                    ViewData["AlertMessage"] = "<p class='alert alert-success'>Your password update is successful.</p>";
                }
                else
                {
                    ViewData["AlertMessage"] = "<p class='alert alert-danger'>Your current password is incorrect.</p>";
                }
            }
            catch (Exception ex)
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + ex.Message + "</p>";
            }
            finally
            {
                con.Close();
            }
            return count;

        }
        #endregion Helpers


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

    }
}