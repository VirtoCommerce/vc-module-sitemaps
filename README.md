# VirtoCommerce.Sitemaps
VirtoCommerce.Sitemaps module represents sitemaps management system.

Key features:
* Managing store sitemaps
* Managing sitemap records

![sitemaps](https://cloud.githubusercontent.com/assets/10347112/21455566/eccaa5f4-c929-11e6-9590-ecaa203ef39b.png)

# Documentation
User guide: [Sitemaps](http://virtocommerce.com/docs/vc2userguide/sitemaps)

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Sitemaps module -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-sitemaps/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install.

# Settings
## General settings
* **Records limit** (default value: **10000**) - sets the maximum number of URLs record per sitemap file
* **Filename separator** (default value: **--**) - sets the sitemap location separator in case of sitemap items number exceeds the **Records limit** parameter value (i.e.: "products.xml" -> "products--1.xml" and "products--2.xml")
* **Search bunch size** (default value: **1000**) - this parameter is using in long-term search processes (i.e. catalog search) to divide search requests and sets the search request bunch size parameter
* **Export/Import description** (default value: **Export/Import sitemaps with all sitemap items**) - sets the description for platform export/import process

## Category links
* **Category page priority** (default value: **0.7**) - sets the value of the sitemap **&lt;priority&gt;** parameter of catalog categories pages
* **Category page update frequency** (default value: **weekly**) - sets the value of the sitemap **&lt;changefreq&gt;** parameter of catalog categories pages

## Product links
* **Product page priority** (default value: **1.0**) - sets the value of the sitemap **&lt;priority&gt;** parameter of catalog products pages
* **Product page update frequency** (default value: **daily**) - sets the value of the sitemap **&lt;changefreq&gt;** parameter of catalog products pages

# License
Copyright (c) Virtosoftware Ltd.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
