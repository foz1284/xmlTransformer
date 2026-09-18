<?xml version="1.0" encoding="UTF-8"?>
<!--
  Teamcenter (PLMXML-shaped) material master -> SAP MATMAS05 IDoc.

  Source contract (namespace http://www.plmxml.org/Schemas/PLMXMLSchema):
    Product/@productId or Item/@itemId     -> E1MARAM/MATNR
    Product/@name, Description, or revision -> E1MAKTM/MAKTX (max 40 chars)
    ProductRevision/@revision              -> not mapped in v1 (no basic-data field)
    UserValue[@title='uom_tag']            -> E1MARAM/MEINS (ISO lookup)
    UserValue[@title='object_type']        -> E1MARAM/MTART (type lookup)
    UserValue[@title='material_group']     -> E1MARAM/MATKL (omitted if empty)
    UserValue[@title='industry_sector']    -> E1MARAM/MBRSH (else $industrySectorDefault)

  Value maps:
    object_type Item|Part -> FERT; Material -> ROH; else $materialTypeDefault
    uom each|EA -> PCE; kg|KG -> KGM; else passthrough
-->
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:plm="http://www.plmxml.org/Schemas/PLMXMLSchema"
                exclude-result-prefixes="plm">

  <xsl:output method="xml" encoding="UTF-8" indent="yes"/>

  <xsl:param name="senderPort" select="'XMLTRANSFORM'"/>
  <xsl:param name="senderPartnerType" select="'LS'"/>
  <xsl:param name="senderPartner" select="'TCENT'"/>
  <xsl:param name="receiverPort" select="'SAPPORT'"/>
  <xsl:param name="receiverPartnerType" select="'LS'"/>
  <xsl:param name="receiverPartner" select="'SAPCLNT100'"/>
  <xsl:param name="materialTypeDefault" select="'FERT'"/>
  <xsl:param name="industrySectorDefault" select="'M'"/>
  <xsl:param name="messageFunction" select="'005'"/>
  <xsl:param name="language" select="'E'"/>
  <xsl:param name="languageIso" select="'EN'"/>
  <xsl:param name="docNumber" select="'0000000000000001'"/>
  <xsl:param name="creationDate" select="''"/>
  <xsl:param name="creationTime" select="''"/>

  <xsl:variable name="material" select="(//plm:Product | //plm:Item)[1]"/>
  <xsl:variable name="revision" select="//plm:ProductRevision[@masterRef = concat('#', $material/@id)] | //plm:ProductRevision[1]"/>

  <xsl:template match="/">
    <MATMAS05>
      <IDOC BEGIN="1">
        <EDI_DC40 SEGMENT="1">
          <TABNAM>EDI_DC40</TABNAM>
          <DOCNUM>
            <xsl:value-of select="$docNumber"/>
          </DOCNUM>
          <DIRECT>2</DIRECT>
          <IDOCTYP>MATMAS05</IDOCTYP>
          <MESTYP>MATMAS</MESTYP>
          <SNDPOR>
            <xsl:value-of select="$senderPort"/>
          </SNDPOR>
          <SNDPRT>
            <xsl:value-of select="$senderPartnerType"/>
          </SNDPRT>
          <SNDPRN>
            <xsl:value-of select="$senderPartner"/>
          </SNDPRN>
          <RCVPOR>
            <xsl:value-of select="$receiverPort"/>
          </RCVPOR>
          <RCVPRT>
            <xsl:value-of select="$receiverPartnerType"/>
          </RCVPRT>
          <RCVPRN>
            <xsl:value-of select="$receiverPartner"/>
          </RCVPRN>
          <CREDAT>
            <xsl:value-of select="$creationDate"/>
          </CREDAT>
          <CRETIM>
            <xsl:value-of select="$creationTime"/>
          </CRETIM>
          <SERIAL>
            <xsl:value-of select="concat($creationDate, $creationTime)"/>
          </SERIAL>
        </EDI_DC40>
        <xsl:apply-templates select="$material"/>
      </IDOC>
    </MATMAS05>
  </xsl:template>

  <xsl:template match="plm:Product | plm:Item">
    <xsl:variable name="matnr">
      <xsl:choose>
        <xsl:when test="normalize-space(@productId)">
          <xsl:value-of select="normalize-space(@productId)"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="normalize-space(@itemId)"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <xsl:variable name="objectType" select="normalize-space(plm:UserData/plm:UserValue[@title='object_type']/@value)"/>
    <xsl:variable name="uomRaw" select="normalize-space(plm:UserData/plm:UserValue[@title='uom_tag']/@value)"/>
    <xsl:variable name="matkl" select="normalize-space(plm:UserData/plm:UserValue[@title='material_group']/@value)"/>
    <xsl:variable name="mbrshRaw" select="normalize-space(plm:UserData/plm:UserValue[@title='industry_sector']/@value)"/>

    <xsl:variable name="maktx">
      <xsl:call-template name="material-description"/>
    </xsl:variable>

    <xsl:variable name="meins">
      <xsl:call-template name="map-uom">
        <xsl:with-param name="uom" select="$uomRaw"/>
      </xsl:call-template>
    </xsl:variable>

    <xsl:variable name="mtart">
      <xsl:call-template name="map-material-type">
        <xsl:with-param name="objectType" select="$objectType"/>
      </xsl:call-template>
    </xsl:variable>

    <xsl:variable name="mbrsh">
      <xsl:choose>
        <xsl:when test="$mbrshRaw">
          <xsl:value-of select="$mbrshRaw"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$industrySectorDefault"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <E1MARAM SEGMENT="1">
      <MSGFN>
        <xsl:value-of select="$messageFunction"/>
      </MSGFN>
      <MATNR>
        <xsl:value-of select="$matnr"/>
      </MATNR>
      <MTART>
        <xsl:value-of select="$mtart"/>
      </MTART>
      <xsl:if test="$mbrsh">
        <MBRSH>
          <xsl:value-of select="$mbrsh"/>
        </MBRSH>
      </xsl:if>
      <xsl:if test="$matkl">
        <MATKL>
          <xsl:value-of select="$matkl"/>
        </MATKL>
      </xsl:if>
      <xsl:if test="$meins">
        <MEINS>
          <xsl:value-of select="$meins"/>
        </MEINS>
      </xsl:if>
      <E1MAKTM SEGMENT="1">
        <MSGFN>
          <xsl:value-of select="$messageFunction"/>
        </MSGFN>
        <SPRAS>
          <xsl:value-of select="$language"/>
        </SPRAS>
        <MAKTX>
          <xsl:value-of select="substring($maktx, 1, 40)"/>
        </MAKTX>
        <SPRAS_ISO>
          <xsl:value-of select="$languageIso"/>
        </SPRAS_ISO>
      </E1MAKTM>
    </E1MARAM>
  </xsl:template>

  <xsl:template name="material-description">
    <xsl:choose>
      <xsl:when test="normalize-space(@name)">
        <xsl:value-of select="normalize-space(@name)"/>
      </xsl:when>
      <xsl:when test="normalize-space(plm:Description)">
        <xsl:value-of select="normalize-space(plm:Description)"/>
      </xsl:when>
      <xsl:when test="normalize-space($revision/@name)">
        <xsl:value-of select="normalize-space($revision/@name)"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="normalize-space($revision/plm:Description)"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="map-material-type">
    <xsl:param name="objectType"/>
    <xsl:variable name="normalized" select="translate($objectType, 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ')"/>
    <xsl:choose>
      <xsl:when test="$normalized = 'ITEM' or $normalized = 'PART'">FERT</xsl:when>
      <xsl:when test="$normalized = 'MATERIAL'">ROH</xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$materialTypeDefault"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="map-uom">
    <xsl:param name="uom"/>
    <xsl:variable name="normalized" select="translate($uom, 'abcdefghijklmnopqrstuvwxyz', 'ABCDEFGHIJKLMNOPQRSTUVWXYZ')"/>
    <xsl:choose>
      <xsl:when test="$normalized = ''"/>
      <xsl:when test="$normalized = 'EACH' or $normalized = 'EA'">PCE</xsl:when>
      <xsl:when test="$normalized = 'KG' or $normalized = 'KGM'">KGM</xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$uom"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
