// <copyright file="ExceptionFilenetCMIS.cs" company="Banco de chile">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BusinessExceptions
{
    using System;

    /// <summary>
    /// Control de excepciones.
    /// </summary>
    public class ExceptionFilenetCMIS : Exception
    {
        private static readonly string DefaultMessage = " ";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionFilenetCMIS"/> class.
        /// crea un nuevo con un mensajo dispuesto por el programador.
        /// </summary>
        public ExceptionFilenetCMIS()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionFilenetCMIS"/> class.
        /// crea un nuevo con un mensajo dispuesto por el programador.
        /// </summary>
        /// <param name="message">Mensaje. </param>
        public ExceptionFilenetCMIS(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionFilenetCMIS"/> class.
        /// crea un nuevo con un mensajo dispuesto por el programador.
        /// </summary>
        /// <param name="innerException">Excepcion. </param>
        public ExceptionFilenetCMIS(Exception innerException)
            : base(DefaultMessage, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionFilenetCMIS"/> class.
        /// crea un nuevo con un mensajo dispuesto por el programador.
        /// </summary>
        /// <param name="message">Mensaje. </param>
        /// <param name="innerException">Excepcion. </param>
        public ExceptionFilenetCMIS(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}