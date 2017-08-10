# VirtoCommerce.Sitemaps
VirtoCommerce.Sitemaps module represents sitemaps management system.

# Use cases
* Add a sitemap for a store
![sitemaps-1](https://cloud.githubusercontent.com/assets/10347112/21457294/d4c051a4-c936-11e6-9580-41b23c6d06fe.png)

* Add a sitemap items (categories, products, vendors, custom sitemap items, etc.) to a sitemap
![sitemaps-2](https://cloud.githubusercontent.com/assets/10347112/21457310/f49e97c4-c936-11e6-9078-0ed84675fa04.png)
![sitemaps-3](https://cloud.githubusercontent.com/assets/10347112/21457327/1d0ef938-c937-11e6-8fda-711b3ad170ce.png)

And after that:
* Download a zip package with pregenerated sitemap XML files and place its content to storefront theme folder manually.  
* Get the sitemaps schema and generate sitemap index file and sitemap files on-the-fly by API call (recommended for small stores, where the number of catalog items/vendors is less than 500)
* Get the sitemaps schema and pregenerate sitemap XML files by scheduled recurring job (recommended for big stores since catalog/vendor search is a long-term process and sitemaps generation may require tens of minutes)

# API calls

```
// Get a collection of sitemap location URLs

[GET] api/sitemaps/schema?storeId=...
```

```
// Get a stream contains a sitemap file XML data

[GET] api/sitemaps/generate?storeId=...&baseUrl=...&sitemapUrl=...
```

# Documentation
User guide: [Sitemaps](http://virtocommerce.com/docs/vc2userguide/sitemaps)

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Sitemaps module -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-sitemaps/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install.

# Settings
### General settings
* **Records limit** (default value: **10000**) - sets the maximum number of URLs record per sitemap file
* **Filename separator** (default value: **--**) - sets the sitemap location separator in case of sitemap items number exceeds the **Records limit** parameter value (i.e.: "products.xml" -> "products--1.xml" and "products--2.xml")
* **Search bunch size** (default value: **1000**) - this parameter is using in long-term search processes (i.e. catalog search) to divide search requests and sets the search request bunch size parameter
* **Export/Import description** (default value: **Export/Import sitemaps with all sitemap items**) - sets the description for platform export/import process

### Category links
* **Category page priority** (default value: **0.7**) - sets the value of the sitemap **&lt;priority&gt;** parameter of catalog categories pages
* **Category page update frequency** (default value: **weekly**) - sets the value of the sitemap **&lt;changefreq&gt;** parameter of catalog categories pages

### Product links
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
