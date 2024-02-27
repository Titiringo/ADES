using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

// Importación de modelos, datos y servicios específicos de la aplicación.
using WebAppCorreo.Models;
using WebAppCorreo.Datos;
using WebAppCorreo.Servicios;

// Definición del espacio de nombres y el controlador.
namespace WebAppCorreo.Controllers
{
    public class InicioController : Controller
    {
        // Acción que muestra una vista selector inicial.
        public ActionResult Selector()
        {
            return View();
        }

        // GET: Inicio/Login
        // Muestra la vista de login.
        public ActionResult Login()
        {
            return View();
        }

        // POST: Inicio/Login
        // Procesa la petición de login.
        [HttpPost]
        public ActionResult Login(string correo, string clave)
        {
            // Valida las credenciales del usuario.
            UsuarioDTO usuario = DBUsuario.Validar(correo, UtilidadServicio.ConvertirSHA256(clave));

            // Verifica el estado del usuario y redirige o muestra mensajes según corresponda.
            if (usuario != null)
            {
                if (!usuario.Confirmado)
                {
                    // Usuario no confirmado.
                    ViewBag.Mensaje = $"Falta confirmar su cuenta. Se le envio un correo a {correo}";
                }
                else if (usuario.Restablecer)
                {
                    // Restablecimiento de cuenta solicitado.
                    ViewBag.Mensaje = $"Se ha solicitado restablecer su cuenta, favor revise su bandeja del correo {correo}";
                }
                else
                {
                    // Credenciales válidas y usuario confirmado, redirige a Home.
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                // Credenciales inválidas.
                ViewBag.Mensaje = "No se encontraron coincidencias";
            }

            return View();
        }

        // GET: Inicio/Registrar
        // Muestra la vista de registro.
        public ActionResult Registrar()
        {
            return View();
        }

        // POST: Inicio/Registrar
        // Procesa la petición de registro de un nuevo usuario.
        [HttpPost]
        public ActionResult Registrar(UsuarioDTO usuario)
        {
            // Verifica que las contraseñas ingresadas coincidan.
            if (usuario.Clave != usuario.ConfirmarClave)
            {
                // Contraseñas no coinciden.
                ViewBag.Mensaje = "Las contraseñas no coinciden";
                return View();
            }

            // Verifica si el correo ya está registrado.
            if (DBUsuario.Obtener(usuario.Correo) == null)
            {
                // Prepara y registra al nuevo usuario.
                usuario.Clave = UtilidadServicio.ConvertirSHA256(usuario.Clave);
                usuario.Token = UtilidadServicio.GenerarToken();
                usuario.Restablecer = false;
                usuario.Confirmado = false;
                bool respuesta = DBUsuario.Registrar(usuario);

                if (respuesta)
                {
                    // Usuario registrado exitosamente. Envía correo de confirmación.
                    string path = HttpContext.Server.MapPath("~/Plantilla/Confirmar.html");
                    string content = System.IO.File.ReadAllText(path);
                    string url = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Headers["host"], "/Inicio/Confirmar?token=" + usuario.Token);

                    string htmlBody = string.Format(content, usuario.Nombre, url);

                    // Configuración y envío del correo de confirmación.
                    CorreoDTO correoDTO = new CorreoDTO()
                    {
                        Para = usuario.Correo,
                        Asunto = "Correo confirmacion",
                        Contenido = htmlBody
                    };

                    bool enviado = CorreoServicio.Enviar(correoDTO);
                    ViewBag.Creado = true;
                    ViewBag.Mensaje = $"Su cuenta ha sido creada. Hemos enviado un mensaje al correo {usuario.Correo} para confirmar su cuenta";
                }
                else
                {
                    // Falla al registrar el usuario.
                    ViewBag.Mensaje = "No se pudo crear su cuenta";
                }
            }
            else
            {
                // Correo ya registrado.
                ViewBag.Mensaje = "El correo ya se encuentra registrado";
            }

            return View();
        }

        // GET: Inicio/Confirmar
        // Acción para confirmar la cuenta de un usuario mediante un token.
        public ActionResult Confirmar(string token)
        {
            ViewBag.Respuesta = DBUsuario.Confirmar(token);
            return View();
        }

        // GET: Inicio/Restablecer
        // Muestra la vista para iniciar el proceso de restablecimiento de contraseña.
        public ActionResult Restablecer()
        {
            return View();
        }

        // POST: Inicio/Restablecer
        // Procesa la petición de restablecimiento de contraseña.
        [HttpPost]
        public ActionResult Restablecer(string correo)
        {
            UsuarioDTO usuario = DBUsuario.Obtener(correo);
            if (usuario != null)
            {
                // Intenta marcar la cuenta para restablecer la contraseña.
                bool respuesta = DBUsuario.RestablecerActualizar(1, usuario.Clave, usuario.Token);

                if (respuesta)
                {
                    // Envía correo para restablecer la contraseña.
                    string path = HttpContext.Server.MapPath("~/Plantilla/Restablecer.html");
                    string content = System.IO.File.ReadAllText(path);
                    string url = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Headers["host"], "/Inicio/Actualizar?token=" + usuario.Token);

                    string htmlBody = string.Format(content, usuario.Nombre, url);

                    // Configuración y envío del correo para restablecimiento.
                    CorreoDTO correoDTO = new CorreoDTO()
                    {
                        Para = correo,
                        Asunto = "Restablecer cuenta",
                        Contenido = htmlBody
                    };

                    bool enviado = CorreoServicio.Enviar(correoDTO);
                    ViewBag.Restablecido = true;
                }
                else
                {
                    // Falla al intentar marcar la cuenta para restablecimiento.
                    ViewBag.Mensaje = "No se pudo restablecer la cuenta";
                }
            }
            else
            {
                // Correo no encontrado.
                ViewBag.Mensaje = "No se encontraron coincidencias con el correo";
            }

            return View();
        }

        // GET: Inicio/Actualizar
        // Muestra la vista para actualizar la contraseña tras restablecimiento.
        public ActionResult Actualizar(string token)
        {
            ViewBag.Token = token;
            return View();
        }

        // POST: Inicio/Actualizar
        // Procesa la actualización de la contraseña tras el restablecimiento.
        [HttpPost]
        public ActionResult Actualizar(string token, string clave, string confirmarClave)
        {
            // Verifica que las contraseñas ingresadas coincidan.
            if (clave != confirmarClave)
            {
                // Contraseñas no coinciden.
                ViewBag.Mensaje = "Las contraseñas no coinciden";
                return View();
            }

            // Actualiza la contraseña del usuario.
            bool respuesta = DBUsuario.RestablecerActualizar(2, UtilidadServicio.ConvertirSHA256(clave), token);

            if (respuesta)
            {
                // Contraseña actualizada exitosamente.
                ViewBag.Actualizado = true;
            }
            else
            {
                // Falla al actualizar la contraseña.
                ViewBag.Mensaje = "No se pudo actualizar la contraseña";
            }

            return View();
        }
    }
}
