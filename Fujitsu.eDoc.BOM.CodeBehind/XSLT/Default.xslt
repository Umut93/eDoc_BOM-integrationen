<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
								xmlns:edoc="http://www.fujitsu.dk/agenda/xml/schemas/2009/03/31/"
								xmlns:ms="urn:schemas-microsoft-com:xslt"
								xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" />

  <!--OutputStyle. (What kind/part of output are we creating)
	Values: 
	All - Used for creating the whole lot (header and all handlingitems),
	SingleItem - Used for overview in the eDoc Agenda site and for printing of each handlingitem,
	Header - Used for printing the header-->
  <xsl:param name="OutputStyle"></xsl:param>
  <!--OutputCssFile. (The link to the configured Css file will be in this param)-->
  <xsl:param name="OutputCssFile"></xsl:param>

  <!--The XSLT transformation is C# enabled, if u need to do special stuff or call any C# functions-->

  <xsl:template match="/">
    <html xmlns="http://www.w3.org/1999/xhtml">
      <head>
        <title>Bilagsliste</title>
        <!--Dynamic link for the css file, value from param-->
        <style type="text/css">
          @page xsltPageMargin
          { /* margin: top højre bund venstre */
          margin: 1.0cm 1.0cm 0.8cm 1.0cm;
          }

          div.xsltPageMargin
          {
          page:xsltPageMargin;
          }

          .xsltOuter
          {
          font-family: Verdana,Times New Roman,serif;
          font-size: 9.5pt;
          width: 475.0pt;
          }

          .NewPage
          {
          page-break-before:always;
          }

          .xsltCaseTitle
          {
          page-break-before:always;
          font-family: Verdana;
          font-size: 16.0pt;
          font-weight: normal;
          }

          .xsltSubTitle
          {
          font-family: Verdana;
          font-size: 12.0pt;
          font-weight: bold;
          color: #365F91;
          }

          .xsltCaseInfo
          {
          font-family:Verdana;
          font-size:9.5pt;
          display: inline-block;
          }

          .tdRightMetaInfo
          {
          text-align: right;
          }

          .tdLeftMetaInfo
          {
          text-align: left;
          }

          .CaseBorder
          {
          page-break-after:always;
          }

          .AttachmentBorder
          {
          border-style: solid hidden solid hidden; border-top-width: thin; border-bottom-width: thin; border-top-color: #4F81BD; border-bottom-color: #4F81BD; position: relative; width: 95%; left: 20px;
          }

          .JournalSheetHeader {
          font-family: Arial, Helvetica, sans-serif;
          font-size: x-large;
          font-weight: bold;
          color: #365F91;
          }
          .auto-style1 {
          height: 20px;
          }
          .auto-style2 {
          width: 136px;
          height: 20px;
          }
          .BlueTableHeader {
          font-size: x-small; font-family: Arial, Helvetica, sans-serif; font-weight: bold; color: #365F91;
          }
          .BlueTableEvenLine {
          font-family: Arial, Helvetica, sans-serif; font-size: x-small; color: #808080 !important;
          }
          .BlueTableOddLine {
          font-family: Arial, Helvetica, sans-serif;
          font-size: x-small;
          color: #808080 !important;
          background-color: #D3DFEE !important;
          }
          .BlueTableOddLine p {
          font-family: Arial, Helvetica, sans-serif;
          font-size: x-small;
          color: #808080 !important;
          background-color: #D3DFEE !important;
          }
        </style>
      </head>
      <body>
        <div class="xsltPageMargin">
          <div class="xsltOuter">
            <p>
              <span class="JournalSheetHeader">Sag</span>
            </p>
            <xsl:for-each select="Case">
              <div class="CaseBorder">
                <table class="xsltCaseInfo" style="width: 100%;">
                  <tr>
                    <td>
                      <xsl:text disable-output-escaping="yes"><![CDATA[&nbsp;&nbsp;&nbsp;]]></xsl:text>
                    </td>
                    <td style=" width: 136px;">Sagsnr.:</td>
                    <td>
                      <xsl:value-of select="CaseNumber"/>
                    </td>
                  </tr>
                  <tr>
                    <td>
                      <xsl:text disable-output-escaping="yes"><![CDATA[&nbsp;&nbsp;&nbsp;]]></xsl:text>
                    </td>
                    <td style=" width: 136px;">Sagstitel:</td>
                    <td>
                      <xsl:value-of select="CaseTitle"/>
                    </td>
                  </tr>
                  <tr>
                    <td>
                      <xsl:text disable-output-escaping="yes"><![CDATA[&nbsp;&nbsp;&nbsp;]]></xsl:text>
                    </td>
                    <td style=" width: 136px;">Sagstype:</td>
                    <td>
                      <xsl:value-of select="CaseTypeDesc"/>
                    </td>
                  </tr>
                  <!--<tr>
                    <td>
                      <xsl:text disable-output-escaping="yes"><![CDATA[&nbsp;&nbsp;&nbsp;]]></xsl:text>
                    </td>
                    <td>Oprettet dato:</td>
                    <td>
                      <xsl:value-of select="util:FormatDate(CreatedDate, 'dd-MM-yyyy hh:mm:ss')"/>
                    </td>
                  </tr>-->
                  <tr>
                    <td>
                      <xsl:text disable-output-escaping="yes"><![CDATA[&nbsp;&nbsp;&nbsp;]]></xsl:text>
                    </td>
                    <td>Sagsansvarlig:</td>
                    <td>
                      <xsl:value-of select="OurRef"/>
                    </td>
                  </tr>
                  <tr>
                    <td>
                      <xsl:text disable-output-escaping="yes"><![CDATA[&nbsp;&nbsp;&nbsp;]]></xsl:text>
                    </td>
                    <td>Afdeling:</td>
                    <td>
                      <xsl:value-of select="OrgUniName"/>
                    </td>
                  </tr>
                </table>
              </div>
              <br />
              <br />
              <br />

              <xsl:if test="AttachmentList">
                <span class="xsltSubTitle">Bilagsliste</span>
                <br />
                <br />

                <table class="AttachmentBorder">
                  <tr class="BlueTableHeader">
                    <td>Fil titel</td>
                    <td>Dokument nr.</td>
                  </tr>

                  <xsl:for-each select="AttachmentList/Attachment">
                    <xsl:variable name="css-class">
                      <xsl:choose>
                        <xsl:when test="position() mod 2 = 0">BlueTableEvenLine</xsl:when>
                        <xsl:otherwise>BlueTableOddLine</xsl:otherwise>
                      </xsl:choose>
                    </xsl:variable>
                    <tr class="{$css-class}">
                      <td>
                        <xsl:value-of select="Title"/>
                      </td>
                      <td>
                        <xsl:value-of select="DocumentNumber"/>
                      </td>
                    </tr>
                  </xsl:for-each>
                </table>
                <br />
                <br />
                <br />
              </xsl:if>
            </xsl:for-each>
          </div>
        </div>
      </body>
    </html>
  </xsl:template>

</xsl:stylesheet>