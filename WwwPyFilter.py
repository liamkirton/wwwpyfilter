# --------------------------------------------------------------------------------
# WwwPyFilter
#
# Copyright ©2008 Liam Kirton <liam@int3.ws>
# --------------------------------------------------------------------------------
# WwwPyFilter.py
#
# Created: 09/01/2008
# --------------------------------------------------------------------------------

import re
import sys

from WwwProxy import ProxyRequest, ProxyResponse

# --------------------------------------------------------------------------------

class WwwPyFilter(object):
	
	# ----------------------------------------------------------------------------
	
	def __init__(self):
		pass
	    
	# ----------------------------------------------------------------------------
	
	def request_filter(self, request):
		request_matcher = re.compile('^([A-Z]+)\s+(.*)\s+HTTP/1.\d')
		if request_matcher.match(request.Header) != None:
			request_groups = request_matcher.search(request.Header).groups()
			print "request_filter(%d, %s, %s)" % (request.Id, request_groups[0], request_groups[1])
			print "\n%s\n" % request.Header
			
			if request.Data != None:
				print "%s\n" % request.Data	
			
		return True
	
	# ----------------------------------------------------------------------------
	
	def response_filter(self, request, response):
		request_matcher = re.compile('^([A-Z]+)\s+(.*)\s+HTTP/1.\d')
		if request_matcher.match(request.Header) != None:
			request_groups = request_matcher.search(request.Header).groups()
			print "response_filter(%d, %s, %s, %s)" % (request.Id, response.Completable, request_groups[0], request_groups[1])
			print "\n%s\n" % response.Header
		
		return True
	
	# ----------------------------------------------------------------------------

# --------------------------------------------------------------------------------
