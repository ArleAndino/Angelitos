﻿using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;

public partial class FlujoGuias : Utilidades.PaginaBase
{
    protected override void OnLoad(EventArgs e)
    {
        MODULO = "Flujo Guias";
        base.OnLoad(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        SetGridFilterMenu(rgGuias.FilterMenu);
        rgGuias.FilterItemStyle.Font.Size = FontUnit.XXSmall;
        rgGuias.FilterMenu.Font.Size = FontUnit.XXSmall;
        if (!IsPostBack)
        {
            if (!Ingresar)
                redirectTo("Default.aspx");
            rgGuias.ClientSettings.Scrolling.AllowScroll = true;
            rgGuias.ClientSettings.Scrolling.UseStaticHeaders = true;
            rgGuias.ShowHeader = true;
            rgGuias.ShowFooter = true;
            ((SiteMaster)Master).SetTitulo("Flujo Guias Varias Aduanas", "Flujo Guías Varias Aduanas");

            mpFlujoGuias.SelectedIndex = 0;
        }
    }

    protected void rgGuias_Init(object sender, EventArgs e)
    {
        var menu = rgGuias.FilterMenu;
        menu.Items.RemoveAt(rgGuias.FilterMenu.Items.Count - 2);
    }

    private bool Ingresar { get { return tienePermiso("INGRESAR"); } }

    protected void rgGuias_NeedDataSource(object source, GridNeedDataSourceEventArgs e)
    {
        LoadGridGuias();
    }

    private void LoadGridGuias()
    {
        try
        {
            conectar();
            var g2 = new GuiasBO(logApp);
            if (User.IsInRole("Supervisores"))
                g2.LoadFLujoGuiasAduana("Supervisores");
            else if (User.IsInRole("Monitoreo"))
                g2.LoadFLujoGuiasAduana("Monitoreo");
            else if (User.IsInRole("Jefe de Monitoreo"))
                g2.LoadFLujoGuiasAduana("Jefe de Monitoreo");
            else if (User.IsInRole("Administradores"))
                g2.LoadFLujoGuiasAduana("Administradores");
            else if (User.IsInRole("Jefe de Operaciones"))
                g2.LoadFLujoGuiasAduana("Jefe de Operaciones");

            rgGuias.DataSource = g2.TABLA;
            //rgGuias.DataBind();
        }
        catch (Exception) { }
        finally
        {
            desconectar();
        }
    }

    protected void rgGuias_ItemCommand(object sender, GridCommandEventArgs e)
    {
        try
        {
            if (e.CommandName == "Sellar")
            {
                conectar();
                GridEditableItem editedItem = e.Item as GridEditableItem;
                edCorrGuia.Value = Convert.ToString(editedItem.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["CorrelativoGuia"]);
                edCodRuta.Value = Convert.ToString(editedItem.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["CodigoRuta"]);
                edCodEstado.Value = Convert.ToString(editedItem.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["CodEstado"]);
                edCodAduana.Value = Convert.ToString(editedItem.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["CodigoAduana"]);
                edTipoFlujo.Value = Convert.ToString(editedItem.OwnerTableView.DataKeyValues[e.Item.ItemIndex]["TipoFlujo"]);
                RadDatePicker fecha = (RadDatePicker)editedItem.FindControl("dtFechaSellado");
                RadTimePicker hora = (RadTimePicker)editedItem.FindControl("dtHoraSellado");
                RadTextBox observacion = (RadTextBox)editedItem.FindControl("edObservacion");
                if (fecha.SelectedDate.HasValue)
                {
                    edFecha.Value = fecha.SelectedDate.Value.ToShortDateString();
                    if (hora.SelectedDate.HasValue)
                        edHora.Value = hora.SelectedDate.Value.ToShortTimeString();
                    else
                        registrarMensaje("Hora no puede estar vacía");
                }
                else
                    registrarMensaje("Fecha no puede estar vacía");

                edObservacion.Value = observacion.Text.Trim();
                if (e.CommandName == "Sellar")
                {
                    sellarEstadoParteI();
                }
            }
        }
        catch (Exception)
        { }
    }

    private void sellarEstadoParteI()
    {
        try
        {
            conectar();
            GuiasBO g = new GuiasBO(logApp);
            TiempoFlujosBO tf = new TiempoFlujosBO(logApp);
            AduanasXRutaBO ar = new AduanasXRutaBO(logApp);
            DateTime fechaIngreso = Convert.ToDateTime(edFecha.Value.Trim() + " " + edHora.Value);
            g.loadGuiaEst(edCorrGuia.Value);
            if (g.totalRegistros > 0)
            {
                if (edCodEstado.Value == "1" || edCodEstado.Value == "5")   //g.MONITOREOREMOTOINICIO
                {
                    //DateTime fechaDGuia = Convert.ToDateTime(g.FECHA);
                    //if (fechaIngreso > fechaDGuia)
                    //{
                    sellarEstado(fechaIngreso.ToString());
                    //}
                    //else
                    //    registrarMensaje("Fecha debe ser mayor que la fecha del estado anterior (" + g.FECHA + ")");
                }
                else
                {
                    int codEstadoAnterior = 0;
                    ar.loadAduanasXRuta(edCodRuta.Value);
                    if (edTipoFlujo.Value.Trim() == "1")
                    {
                        if (ar.totalRegistros == 0 & edCodEstado.Value.ToString() == "8")
                            codEstadoAnterior = int.Parse(edCodEstado.Value) - 3;
                        else
                            codEstadoAnterior = int.Parse(edCodEstado.Value) - 1;   //verificar estadoAnterior cuando hay 2 aduanas
                    }
                    else if (edTipoFlujo.Value.Trim() == "2")
                    {
                        if (ar.totalRegistros == 0)
                        {
                            if (edCodEstado.Value == "1")
                                codEstadoAnterior = 0;
                            else if (edCodEstado.Value == "5")
                                codEstadoAnterior = 1;
                            else if (edCodEstado.Value == "8")
                                codEstadoAnterior = 5;
                            else if (edCodEstado.Value == "10")
                                codEstadoAnterior = 8;
                            //else
                            //    codEstadoAnterior = int.Parse(edCodEstado.Value) - 1;
                        }
                        else
                        {
                            if (edCodEstado.Value == "1")
                                codEstadoAnterior = 0;
                            else if (edCodEstado.Value == "5")
                                codEstadoAnterior = 1;
                            else if (edCodEstado.Value == "6")
                                codEstadoAnterior = 5;
                            else if (edCodEstado.Value == "7")
                                codEstadoAnterior = 6;
                            else if (edCodEstado.Value == "8")
                                codEstadoAnterior = 7;
                            else if (edCodEstado.Value == "10")
                                codEstadoAnterior = 8;
                            //else
                            //    codEstadoAnterior = int.Parse(edCodEstado.Value) - 1;
                        }
                    }
                    tf.loadMaxFechaTiempoFlujos2(edCorrGuia.Value, codEstadoAnterior.ToString());
                    if (tf.totalRegistros > 0 & (tf.FECHA != null & tf.FECHA != ""))
                    {
                        DateTime fechaDTiempoFlujo = Convert.ToDateTime(tf.FECHA);
                        if (fechaIngreso > fechaDTiempoFlujo)
                        {
                            sellarEstado(fechaIngreso.ToString());
                        }
                        else
                            registrarMensaje("Fecha debe ser mayor que la fecha del estado anterior (" + tf.FECHA + ")");
                    }
                    else
                        registrarMensaje("Tiempo Flujo estado anterior no existe");
                }
            }
            else
                registrarMensaje("Guía no existe");
        }
        catch (Exception)
        { }
        finally
        {
            desconectar();
        }
    }

    private void sellarEstado(string fechaIngreso)
    {
        try
        {
            string nuevoCorrelativo = "0";
            string codigoEstado = "";
            conectar();
            TiempoFlujosBO bo = new TiempoFlujosBO(logApp);
            AduanasXRutaBO ar = new AduanasXRutaBO(logApp);
            TiempoFlujosBO tf1 = new TiempoFlujosBO(logApp);
            TiempoFlujosBO tf2 = new TiempoFlujosBO(logApp);
            GuiasBO g = new GuiasBO(logApp);
            UnidadesBO u = new UnidadesBO(logApp);
            MotoristasBO m = new MotoristasBO(logApp);
            CodigosBO co = new CodigosBO(logApp);
            AduanasBO a = new AduanasBO(logApp);

            if (edCodEstado.Value != "14")
            {
                if (edTipoFlujo.Value == "1")   //flujo completo
                {
                    #region Flujo Completo
                    //if (edCodEstado.Value == "6" || edCodEstado.Value == "7")
                    //{
                    ar.loadAduanasXRuta(edCodRuta.Value);
                    if (ar.totalRegistros > 0)
                    {
                        if (edCodEstado.Value == "6" || edCodEstado.Value == "7")
                        {
                            //cuando hay mas de una aduana que reitere en 6 y 7
                            tf1.loadFlujoGuiaCodFlujo(edCorrGuia.Value, "6");
                            tf2.loadFlujoGuiaCodFlujo(edCorrGuia.Value, "7");
                            if (tf1.totalRegistros < ar.totalRegistros)
                            {
                                if (tf2.totalRegistros == tf1.totalRegistros)
                                    codigoEstado = "7";
                                else
                                    codigoEstado = "6";
                            }
                            else
                                codigoEstado = "8";
                        }
                        else if (edCodEstado.Value == "10")
                            codigoEstado = (int.Parse(edCodEstado.Value) + 4).ToString();
                        else
                            codigoEstado = (int.Parse(edCodEstado.Value) + 1).ToString();
                    }
                    else
                    {
                        if (edCodEstado.Value == "5")
                        {
                            codigoEstado = (int.Parse(edCodEstado.Value) + 3).ToString();
                            edCodAduana.Value = "";
                        }
                        else if (edCodEstado.Value == "10")
                        {
                            codigoEstado = (int.Parse(edCodEstado.Value) + 4).ToString();
                            edCodAduana.Value = "";
                        }
                        else
                        {
                            codigoEstado = (int.Parse(edCodEstado.Value) + 1).ToString();
                            edCodAduana.Value = "";
                        }
                    }

                    bo.getMaxCorrelativo(edCorrGuia.Value);
                    if (bo.totalRegistros > 0)
                        nuevoCorrelativo = (int.Parse(bo.CORRELATIVOGUIA) + 1).ToString();
                    else
                        nuevoCorrelativo = "1";
                    #endregion
                }
                else if (edTipoFlujo.Value == "2")  //semi flujo
                {
                    #region Semiflujo
                    //if (edCodEstado.Value == "6" || edCodEstado.Value == "7")
                    //{
                    ar.loadAduanasXRuta(edCodRuta.Value);
                    if (ar.totalRegistros > 0)
                    {
                        if (edCodEstado.Value == "6" || edCodEstado.Value == "7")
                        {
                            //cuando hay mas de una aduana que reitere en 6 y 7
                            tf1.loadFlujoGuiaCodFlujo(edCorrGuia.Value, "6");
                            tf2.loadFlujoGuiaCodFlujo(edCorrGuia.Value, "7");
                            if (tf1.totalRegistros < ar.totalRegistros)
                            {
                                if (tf2.totalRegistros == tf1.totalRegistros & tf1.totalRegistros == 0)
                                {
                                    codigoEstado = "7";
                                    nuevoCorrelativo = "6";
                                }
                                else if (tf2.totalRegistros != tf1.totalRegistros & tf2.totalRegistros == 0)
                                {
                                    codigoEstado = "6";
                                    nuevoCorrelativo = "7";
                                }
                                else if (tf2.totalRegistros == tf1.totalRegistros & tf1.totalRegistros == 1)
                                {
                                    codigoEstado = "7";
                                    nuevoCorrelativo = "8";
                                }
                                //else if (tf2.totalRegistros != tf1.totalRegistros & tf2.totalRegistros == 0)
                                //{
                                //    codigoEstado = "6";
                                //    nuevoCorrelativo = "7";
                                //}
                            }
                            else
                            {
                                codigoEstado = "8";
                                if (ar.totalRegistros == 1)
                                    nuevoCorrelativo = "7";
                                else if (ar.totalRegistros == 2)
                                    nuevoCorrelativo = "9";
                            }
                        }
                        else
                        {
                            if (edCodEstado.Value == "1")
                            {
                                codigoEstado = "5";
                                nuevoCorrelativo = "1";
                            }
                            else if (edCodEstado.Value == "5")
                            {
                                codigoEstado = "6";
                                nuevoCorrelativo = "5";
                            }
                            else if (edCodEstado.Value == "8")
                            {
                                codigoEstado = "10";
                                nuevoCorrelativo = "8";
                            }
                            else if (edCodEstado.Value == "10")
                            {
                                codigoEstado = "14";
                                nuevoCorrelativo = "10";
                            }
                        }
                    }
                    else
                    {
                        if (edCodEstado.Value == "1")
                        {
                            codigoEstado = "5";
                            edCodAduana.Value = "";
                            nuevoCorrelativo = "1";
                        }
                        else if (edCodEstado.Value == "5")
                        {
                            codigoEstado = "8";
                            edCodAduana.Value = "";
                            nuevoCorrelativo = "5";
                        }
                        else if (edCodEstado.Value == "8")
                        {
                            codigoEstado = "10";
                            edCodAduana.Value = "";
                            nuevoCorrelativo = "8";
                        }
                        else if (edCodEstado.Value == "10")
                        {
                            codigoEstado = "14";
                            edCodAduana.Value = "";
                            nuevoCorrelativo = "10";
                        }
                    }
                    #endregion
                }

                bo.loadTiempoFlujosNewLine(edCorrGuia.Value, edCodEstado.Value, edCodAduana.Value);
                if (bo.totalRegistros <= 0)
                {
                    bo.newLine();
                    bo.CODIGOGUIA = edCorrGuia.Value;
                    bo.CODFLUJO = edCodEstado.Value;
                    bo.CODIGOADUANA = edCodAduana.Value;
                    bo.FECHA = fechaIngreso;
                    bo.OBSERVACION = edObservacion.Value.Trim();
                    bo.CORRELATIVOGUIA = nuevoCorrelativo;
                    bo.ELIMINADO = "0";
                    bo.IDUSUARIO = Session["IdUsuario"].ToString();
                    bo.FECHASISTEMA = DateTime.Now.ToString();
                    bo.commitLine();
                    bo.actualizar();
                }

                g.loadGuiaFlujo(edCorrGuia.Value);
                if (g.totalRegistros > 0)
                {
                    g.CODESTADO = codigoEstado;
                    g.actualizar();

                    //////////nuevo//////////
                    if (int.Parse(edCodEstado.Value) >= 10)
                    {
                        try
                        {
                            u.loadUnidadG(g.CODIGOUNIDAD);
                            if (u.totalRegistros > 0)
                            {
                                u.CODESTADO = "0";
                                u.actualizar();
                            }
                        }
                        catch { }

                        try
                        {
                            m.loadMotoristaG(g.CODIGOMOTORISTA);
                            if (m.totalRegistros > 0)
                            {
                                m.CODESTADO = "0";
                                m.actualizar();
                            }
                        }
                        catch { }
                    }
                }

                co.loadAllCampos("FLUJOTERRESTRE", edCodEstado.Value);
                a.loadAduanas(edCodAduana.Value);
                if (a.totalRegistros > 0)
                {
                    registrarMensaje(co.DESCRIPCION + " " + a.NOMBREADUANA + " actualizado correctamente");
                    llenarBitacora("Se selló el estado " + co.DESCRIPCION + " " + a.NOMBREADUANA + " en guía " + edCorrGuia.Value.Trim() + " con fecha " + bo.FECHA, Session["IdUsuario"].ToString());
                }
                else
                {
                    registrarMensaje(co.DESCRIPCION + " actualizado correctamente");
                    llenarBitacora("Se selló el estado " + co.DESCRIPCION + " en guía " + edCorrGuia.Value.Trim() + " con fecha " + bo.FECHA, Session["IdUsuario"].ToString());
                }
                rgGuias.Rebind();
            }
            else
            {
                registrarMensaje("Flujo de Guía ya está finalizado");
                rgGuias.Rebind();
            }
        }
        catch (Exception)
        { }
        finally
        {
            desconectar();
        }
    }

    protected void btnLimpiar_Click(object sender, ImageClickEventArgs e)
    {
        try
        {
            foreach (GridDataItem grdItem in rgGuias.Items)
            {
                RadDatePicker dtFecha = (RadDatePicker)grdItem.FindControl("dtFechaSellado");
                RadTimePicker dtHora = (RadTimePicker)grdItem.FindControl("dtHoraSellado");
                RadTextBox edObservacion = (RadTextBox)grdItem.FindControl("edObservacion");
                dtFecha.Clear();
                dtHora.Clear();
                edObservacion.Text = "";
            }
        }
        catch (Exception)
        { }
    }
}