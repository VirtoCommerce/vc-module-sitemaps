# Virto Commerce Sitemaps Module

[![CI status](https://github.com/VirtoCommerce/vc-module-sitemaps/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-sitemaps/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-sitemaps&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-sitemaps) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-sitemaps&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-sitemaps) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-sitemaps&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-sitemaps) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-sitemaps&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-sitemaps)

Sitemaps are an easy way for webmasters to inform search engines about the pages on their sites that are available for crawling. In its simplest form, a Sitemap is an XML file that lists URLs for a site along with additional metadata about each URL (when it was last updated, how often it usually changes and how important it is, relative to other URLs in the site) so that search engines could crawl the site more intelligently.

Web crawlers usually discover pages from links within the site and from other sites. Sitemaps supplement this data to allow crawlers, that support Sitemaps, to pick up all URLs in the Sitemap and learn about those URLs using the associated metadata. Using the Sitemap protocol does not guarantee that web pages are included in search engines, but provides hints for web crawlers to do a better job while crawling your site.

Virto Commerce provides multiple sitemap files, each sitemap file must include no more than 10,000 URLs (by default, maximum value - 50000 URLs) and must be no larger than 50MB (52,428,800 bytes). Each sitemap file will be placed in a sitemap index file "sitemap.xml". In case of sitemap file has more than maximum records number, it would be separated to several sitemap files, i.e.: "products.xml" sitemap file with 15000 records would be transformed to "products--1.xml" (10000 records) and "products--2.xml" (5000 records). Each of these partial sitemap files would be included in sitemap index file too.

## Key features

* Async Sitemap generator
* Support big catalogs 
* Export products and categories
* Rich API

## Documentation

* [Sitemaps module users documentation](https://docs.virtocommerce.org/platform/user-guide/sitemaps/overview/)
* [REST API](https://virtostart-demo-admin.govirto.com/docs/index.html?urls.primaryName=VirtoCommerce.Sitemaps)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-sitemaps/)


## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-sitemaps/releases/latest)

## License

Copyright (c) Virtosoftware Ltd.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
