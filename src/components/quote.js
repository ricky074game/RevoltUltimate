import React from 'react'

import PropTypes from 'prop-types'

import './quote.css'

const Quote = (props) => {
  return (
    <div className={`quote-quote1 ${props.rootClassName} `}>
      <div className="quote-quote2">
        <span className="quote-quote3">{props.quote}</span>
      </div>
      <div className="quote-people">
        <div className="quote-person">
          <img
            alt="person-avatar"
            src={props.avatar}
            className="quote-avatar"
          />
          <div className="quote-person-details">
            <span className="quote-text1">{props.author}</span>
            <span>{props.title}</span>
          </div>
        </div>
      </div>
    </div>
  )
}

Quote.defaultProps = {
  quote:
    '“I love lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliquaation ullamco 100%."',
  rootClassName: '',
  title: 'Manager @Vista Social',
  author: 'Andy Smith',
  avatar: '/pastedimage-8jmb-200w.png',
}

Quote.propTypes = {
  quote: PropTypes.string,
  rootClassName: PropTypes.string,
  title: PropTypes.string,
  author: PropTypes.string,
  avatar: PropTypes.string,
}

export default Quote
